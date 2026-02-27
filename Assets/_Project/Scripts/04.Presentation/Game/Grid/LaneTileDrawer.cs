using System;
using AES.Tools;
using AES.Tools.TBC.Collections;
using ParkingCat;
using ParkingCat.Game;
using ParkingCat.LevelEditor.Util;
using ParkingCat.Stage.DTOs;
using ParkingCat.ValueObjects;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LaneTileDrawer : ContextListenerBase
{
    [Header("Lane Tilemaps (split, cellSize = 1.0)")]
    [SerializeField] Tilemap laneTilemapH; // (—) 전용, transform.y += 0.5
    [SerializeField] Tilemap laneTilemapV; // (|) 전용, transform.x += 0.5

    [Header("Lane Tiles")]
    [SerializeField] TileBase laneH;
    [SerializeField] TileBase laneV;

    [Header("Lane Detect")]
    [Tooltip("진행방향으로 최소 몇 칸 이상 이어져야 라인 생성")]
    [SerializeField] int runMin = 1;

    [Tooltip("폭(차선 수)이 최소 몇 줄 이상일 때 라인 생성. (2면 2차선부터)")]
    [SerializeField] int minThicknessForLine = 2;

    [Header("Junction/branch exclude")]
    [Tooltip("0이면 교차로 직전까지 붙임, 1이면 한 칸 띄움")]
    [SerializeField] int endGapCells = 0;

    [Header("World offset (cellSize=1 기준)")]
    [SerializeField] float boundaryOffset = 0.5f;

    [Header("OneWay Tilemaps (optional)")]
    [SerializeField] Tilemap oneWayTilemapH; // optional
    [SerializeField] Tilemap oneWayTilemapV; // optional

    [Header("OneWay Tiles (optional)")]
    [SerializeField] TileBase oneWayArrowH; // → 기본 타일
    [SerializeField] TileBase oneWayArrowV; // ↑ 기본 타일

    [Tooltip("몇 칸마다 화살표 1개")]
    [SerializeField] int oneWaySpacingCells = 2;

    bool _offsetApplied;
    CompositeDisposable _disposable;

    StageRunDto.OneWayLaneDto[] _oneWays = Array.Empty<StageRunDto.OneWayLaneDto>();

    void Awake() => ApplyOffsetOnce();

    void ApplyOffsetOnce()
    {
        if (_offsetApplied) return;
        _offsetApplied = true;

        if (boundaryOffset == 0f) return;

        if (laneTilemapH != null)
        {
            var t = laneTilemapH.transform;
            t.position = new Vector3(t.position.x, t.position.y + boundaryOffset, t.position.z);
        }

        if (laneTilemapV != null)
        {
            var t = laneTilemapV.transform;
            t.position = new Vector3(t.position.x + boundaryOffset, t.position.y, t.position.z);
        }

        if (oneWayTilemapH != null)
        {
            var t = oneWayTilemapH.transform;
            t.position = new Vector3(t.position.x + boundaryOffset, t.position.y, t.position.z);
        }

        if (oneWayTilemapV != null)
        {
            var t = oneWayTilemapV.transform;
            t.position = new Vector3(t.position.x, t.position.y + boundaryOffset, t.position.z);
        }
    }

    static Matrix4x4 Rot180()
        => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 180f), Vector3.one);

    void RedrawFromVm(GameViewModel vm)
    {
        var grid = vm.Grid01.Value;
        if (grid == null) return;

        _oneWays = vm.OneWayLanes.Value ?? Array.Empty<StageRunDto.OneWayLaneDto>();
        DrawAll(grid);
    }

    public void DrawAll(int[,] grid01)
    {
        ApplyOffsetOnce();

        if (grid01 == null)
            return;

        if (laneTilemapH == null || laneTilemapV == null || laneH == null || laneV == null)
        {
            Debug.LogWarning("[LaneTileDrawer] Missing lane tilemaps/tiles.");
            return;
        }

        laneTilemapH.ClearAllTiles();
        laneTilemapV.ClearAllTiles();
        oneWayTilemapH?.ClearAllTiles();
        oneWayTilemapV?.ClearAllTiles();

        int h = grid01.GetLength(0);
        int w = grid01.GetLength(1);

        bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;
        bool IsRoad(int x, int y) => InBounds(x, y) && grid01[y, x] == 1;

        int RunLen(int x, int y, int dx, int dy)
        {
            int len = 0;
            while (IsRoad(x + dx * (len + 1), y + dy * (len + 1))) len++;
            return len;
        }

        void Score(int x, int y, out int sh, out int sv)
        {
            int lh = RunLen(x, y, -1, 0);
            int rh = RunLen(x, y, 1, 0);
            int dv = RunLen(x, y, 0, -1);
            int uv = RunLen(x, y, 0, 1);
            sh = lh + rh;   // 좌우로 얼마나 이어지나
            sv = dv + uv;   // 상하로 얼마나 이어지나
        }

        // =========================================================
        // [핵심 수정] 방향 판정 로직 교체
        //
        // 문제(2번 상태):
        // - 길이(sh/sv)만 보면, 넓은 블록 내부에서도 세로 통로(기둥) 때문에 sv가 커져
        //   세로로 오판정되어 V 라인이 블록 전체에 퍼짐.
        //
        // 해결:
        // - 둘 다 연결(sh>0 && sv>0)인 셀은 "교차/접합" 성격이므로
        //   '폭(두께) 후보'를 비교해서 더 "좁은 쪽"을 진행방향으로 선택.
        //   (넓은 블록: 세로방향 두께(spanY)가 가로방향 두께(spanX)보다 작아 H로 고정)
        //   (세로 기둥: spanX(기둥 폭)가 spanY(기둥 길이)보다 작아 V로 고정)
        //
        // - 동률은 가로 우선(요구한 1번 형태 쪽으로 안정화).
        // =========================================================
        bool IsHorizLike(int x, int y)
        {
            if (!IsRoad(x, y)) return false;

            Score(x, y, out int sh, out int sv);
            if (sh <= 0) return false; // 좌/우로 1칸도 못가면 가로 후보 아님

            if (sv <= 0) return true;  // 순수 가로

            // 둘 다 연결이면 "폭(두께)" 비교로 결정
            int spanX = sh + 1; // x방향 연속 길이(=세로 밴드의 두께 후보)
            int spanY = sv + 1; // y방향 연속 길이(=가로 밴드의 두께 후보)

            // 가로는 spanY가 더 작거나 같을 때(동률 포함) 선택
            return spanY <= spanX;
        }

        bool IsVertLike(int x, int y)
        {
            if (!IsRoad(x, y)) return false;

            Score(x, y, out int sh, out int sv);
            if (sv <= 0) return false; // 상/하로 1칸도 못가면 세로 후보 아님

            if (sh <= 0) return true;  // 순수 세로

            int spanX = sh + 1;
            int spanY = sv + 1;

            // 세로는 spanX가 "엄격히 더 작을 때"만 선택 (동률은 가로가 가져감)
            return spanX < spanY;
        }

        int ThicknessDown_ForH(int x, int y0)
        {
            int t = 0;
            while (y0 + t < h && IsHorizLike(x, y0 + t)) t++;
            return t;
        }

        int ThicknessRight_ForV(int x0, int y)
        {
            int t = 0;
            while (x0 + t < w && IsVertLike(x0 + t, y)) t++;
            return t;
        }

        bool BandOkAtX_ForH(int x, int y0, int th)
        {
            for (int k = 0; k < th; k++)
                if (!IsHorizLike(x, y0 + k))
                    return false;

            return true;
        }

        bool BandOkAtY_ForV(int x0, int y, int th)
        {
            for (int k = 0; k < th; k++)
                if (!IsVertLike(x0 + k, y))
                    return false;

            return true;
        }

        bool IsJunctionColumn_H(int x, int y0, int th)
        {
            // 1) 밴드 바깥으로 튀는 도로
            if (IsRoad(x, y0 - 1) || IsRoad(x, y0 + th))
                return true;

            // 2) 같은 폭 분기: "밴드 내부"가 아니라 "밴드 바깥"으로 연결될 때만 분기
            for (int k = 0; k < th; k++)
            {
                int y = y0 + k;
                if (!IsRoad(x, y)) continue;

                int upY = y - 1;
                int downY = y + 1;

                bool upOutsideBand = upY < y0 || upY >= y0 + th;
                bool downOutsideBand = downY < y0 || downY >= y0 + th;

                if ((upOutsideBand && IsRoad(x, upY)) ||
                    (downOutsideBand && IsRoad(x, downY)))
                    return true;
            }

            return false;
        }

        bool IsJunctionRow_V(int x0, int y, int th)
        {
            // 1) 밴드 바깥으로 튀는 도로
            if (IsRoad(x0 - 1, y) || IsRoad(x0 + th, y))
                return true;

            // 2) 같은 폭 분기: 밴드 바깥으로 좌/우 연결될 때만 분기
            for (int k = 0; k < th; k++)
            {
                int x = x0 + k;
                if (!IsRoad(x, y)) continue;

                int leftX = x - 1;
                int rightX = x + 1;

                bool leftOutsideBand = leftX < x0 || leftX >= x0 + th;
                bool rightOutsideBand = rightX < x0 || rightX >= x0 + th;

                if ((leftOutsideBand && IsRoad(leftX, y)) ||
                    (rightOutsideBand && IsRoad(rightX, y)))
                    return true;
            }

            return false;
        }

        bool NearGapH(int x, int y0, int th)
        {
            if (endGapCells <= 0) return false;

            for (int d = 1; d <= endGapCells; d++)
                if (IsJunctionColumn_H(x - d, y0, th) || IsJunctionColumn_H(x + d, y0, th))
                    return true;

            return false;
        }

        bool NearGapV(int x0, int y, int th)
        {
            if (endGapCells <= 0) return false;

            for (int d = 1; d <= endGapCells; d++)
                if (IsJunctionRow_V(x0, y - d, th) || IsJunctionRow_V(x0, y + d, th))
                    return true;

            return false;
        }

        // =========================
        // 공용 밴드 드로우
        // =========================
        void DrawBands(
            bool horizontal,
            Tilemap target,
            TileBase tile,
            Func<int, int, bool> IsLike,
            Func<int, int, int> ThicknessAt, // (a, b) -> thickness
            Func<int, int, int, bool> BandOkAt, // (a, b0, th) -> bool
            Func<int, int, int, bool> IsJunctionAt, // (a, b0, th) -> bool
            Func<int, int, int, bool> NearGapAt // (a, b0, th) -> bool
        )
        {
            int AMax = horizontal ? w : h; // 진행축
            int BMax = horizontal ? h : w; // 두께축(스캔 시작축)

            // b0 = 밴드 시작(Top/Left)
            for (int b0 = 0; b0 < BMax; b0++)
                for (int a = 0; a < AMax; a++)
                {
                    int x = horizontal ? a : b0;
                    int y = horizontal ? b0 : a;

                    if (!IsLike(x, y)) continue;

                    // 밴드 시작(Top/Left)만 처리: 이전 b에서 Like면 스킵
                    int prevX = horizontal ? a : (b0 - 1);
                    int prevY = horizontal ? (b0 - 1) : a;
                    if (b0 > 0 && IsLike(prevX, prevY)) continue;

                    int th = horizontal ? ThicknessAt(a, b0) : ThicknessAt(b0, a);
                    if (th < minThicknessForLine) continue;

                    // 시작점 중복 방지: 이전 a에 같은 밴드가 이어지고 junction 아니면 스킵
                    if (a > 0)
                    {
                        int prevA = a - 1;

                        bool sameBandPrevA = horizontal
                            ? BandOkAt(prevA, b0, th)
                            : BandOkAt(b0, prevA, th);

                        bool junctionPrevA = horizontal
                            ? IsJunctionAt(prevA, b0, th)
                            : IsJunctionAt(b0, prevA, th);

                        if (sameBandPrevA && !junctionPrevA)
                            continue;
                    }

                    // run
                    int run = 0;

                    while (a + run < AMax)
                    {
                        bool ok = horizontal
                            ? BandOkAt(a + run, b0, th)
                            : BandOkAt(b0, a + run, th);

                        if (!ok) break;
                        run++;
                    }

                    if (run < runMin) continue;

                    // place
                    for (int da = 0; da < run; da++)
                    {
                        int aa = a + da;

                        bool junction = horizontal
                            ? IsJunctionAt(aa, b0, th)
                            : IsJunctionAt(b0, aa, th);

                        bool nearGap = horizontal
                            ? NearGapAt(aa, b0, th)
                            : NearGapAt(b0, aa, th);

                        if (junction || nearGap) continue;

                        // 두께 th면 경계선은 th-1개
                        for (int s = 1; s <= th - 1; s++)
                        {
                            int px = horizontal ? aa : (b0 + s);
                            int py = horizontal ? (b0 + s) : aa;
                            target.SetTile(new Vector3Int(px, py, 0), tile);
                        }
                    }
                }
        }

        DrawBands(
            horizontal: true,
            target: laneTilemapH,
            tile: laneH,
            IsLike: IsHorizLike,
            ThicknessAt: (a, b0) => ThicknessDown_ForH(a, b0),
            BandOkAt: (a, b0, th) => BandOkAtX_ForH(a, b0, th),
            IsJunctionAt: (a, b0, th) => IsJunctionColumn_H(a, b0, th),
            NearGapAt: (a, b0, th) => NearGapH(a, b0, th)
        );

        DrawBands(
            horizontal: false,
            target: laneTilemapV,
            tile: laneV,
            IsLike: IsVertLike,
            ThicknessAt: (b0, a) => ThicknessRight_ForV(b0, a),
            BandOkAt: (b0, a, th) => BandOkAtY_ForV(b0, a, th),
            IsJunctionAt: (b0, a, th) => IsJunctionRow_V(b0, a, th),
            NearGapAt: (b0, a, th) => NearGapV(b0, a, th)
        );

        // =========================
        // OneWay arrows (optional)
        // =========================
        if (_oneWays != null && _oneWays.Length > 0 &&
            (oneWayTilemapH != null || oneWayTilemapV != null) &&
            (oneWayArrowH != null || oneWayArrowV != null))
        {
            // RoadBandDetector는 1D road01을 받으므로 flatten
            var road01 = new int[w * h];

            for (int yy = 0; yy < h; yy++)
                for (int xx = 0; xx < w; xx++)
                    road01[yy * w + xx] = grid01[yy, xx];

            for (int i = 0; i < _oneWays.Length; i++)
            {
                var ow = _oneWays[i];
                if (ow == null) continue;

                // 현재 grid 기준으로 band 재탐지(유효하면 그리기)
                if (!RoadBandDetector.TryGetBand(road01, w, h, ow.StartCell, endGapCells, out var band))
                    continue;

                int th = Mathf.Max(1, band.Thickness);
                int laneIndex = Mathf.Clamp(ow.LaneIndex, 0, th - 1);

                if (band.IsHorizontal)
                {
                    if (oneWayTilemapH == null || oneWayArrowH == null) continue;

                    int y = band.StartCell.y + laneIndex;

                    int step = Mathf.Max(1, oneWaySpacingCells);
                    int start = step / 2;

                    for (int dx = start; dx < band.Length; dx += step)
                    {
                        int x = band.StartCell.x + dx;
                        if (!InBounds(x, y)) continue;

                        var pos = new Vector3Int(x, y, 0);
                        oneWayTilemapH.SetTile(pos, oneWayArrowH);

                        if (ow.AllowedDir == Dir4.Left)
                            oneWayTilemapH.SetTransformMatrix(pos, Rot180());
                    }
                }
                else
                {
                    if (oneWayTilemapV == null || oneWayArrowV == null) continue;

                    int x = band.StartCell.x + laneIndex;

                    int step = Mathf.Max(1, oneWaySpacingCells);
                    int start = step / 2;

                    for (int dy = start; dy < band.Length; dy += step)
                    {
                        int y = band.StartCell.y + dy;
                        if (!InBounds(x, y)) continue;

                        var pos = new Vector3Int(x, y, 0);
                        oneWayTilemapV.SetTile(pos, oneWayArrowV);

                        if (ow.AllowedDir == Dir4.Down)
                            oneWayTilemapV.SetTransformMatrix(pos, Rot180());
                    }
                }
            }
        }

        laneTilemapH.RefreshAllTiles();
        laneTilemapV.RefreshAllTiles();
        oneWayTilemapH?.RefreshAllTiles();
        oneWayTilemapV?.RefreshAllTiles();
    }

    protected override void OnContextAvailable(IBindingContext ctx)
    {
        var vm = ctx.GetValue() as GameViewModel;
        if (vm == null) return;

        _disposable = new CompositeDisposable();
        vm.Grid01.Subscribe(_ =>
        {
            RedrawFromVm(vm);
        }).AddTo(_disposable);
        vm.OneWayLanes.Subscribe(_ => RedrawFromVm(vm)).AddTo(_disposable);

        RedrawFromVm(vm);
    }

    protected override void OnContextUnavailable()
    {
        _disposable?.Dispose();
        _disposable = null;
    }
}
