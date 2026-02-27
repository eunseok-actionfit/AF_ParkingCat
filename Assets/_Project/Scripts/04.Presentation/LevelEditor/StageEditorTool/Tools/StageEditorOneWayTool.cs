using ParkingCat.LevelEditor.StageEditorTool.Context;
using ParkingCat.LevelEditor.Util;
using UnityEngine;

namespace ParkingCat.LevelEditor.StageEditorTool.Tools
{
    public sealed class StageEditorOneWayTool : IStageEditorTool
    {
        Dir4 _allowed = Dir4.Right;

        const int EndGapCells = 0;

        public void OnEnter(StageEditorToolContext ctx)
        {
            // 초기 힌트 숨김
            ctx?.SetHintVisible?.Invoke(false);
        }

        public void OnExit(StageEditorToolContext ctx)
        {
            // 툴 나갈 때 힌트 숨김
            ctx?.SetHintVisible?.Invoke(false);
        }

        public void Tick(StageEditorToolContext ctx)
        {
            if (ctx?.Cfg == null) return;
            var cfg = ctx.Cfg;

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                _allowed = _allowed.RotateCW();

            if (!UnityEngine.Input.GetMouseButtonDown(0)) return;
            if (!ctx.TryGetMouseCell(out var cell)) return;

            cfg.PruneOneWayLanesIfHiddenByRoad();

            // 1) 기존 oneWay 클릭 삭제
            if (TryPickExistingOneWayIndexAtCell(cfg, cell, out int pickedIndex))
            {
                ctx.PushUndo?.Invoke();
                cfg.OneWayLanes.RemoveAt(pickedIndex);

                cfg.TouchRoad();
                ctx.RefreshPreview?.Invoke();

                ctx.SetHintVisible?.Invoke(false);
                return;
            }

            // 2) Road01 기반 band 탐지
            int w = cfg.Width.Value;
            int h = cfg.Height.Value;
            var road01 = cfg.Road01.Value;
            if (road01 == null || road01.Length != w * h) return;

            if (!RoadBandDetector.TryGetBand(road01, w, h, cell, EndGapCells, out var band))
                return;

            // ===== 축 불일치면 입력 차단 + 힌트 =====
            if (band.IsHorizontal && !IsHorizontal(_allowed))
            {
                ctx.SetHintText?.Invoke("가로 도로는 Left/Right 방향만 설정 가능 (R로 변경)");
                ctx.SetHintVisible?.Invoke(true);
                return;
            }
            if (!band.IsHorizontal && !IsVertical(_allowed))
            {
                ctx.SetHintText?.Invoke("세로 도로는 Up/Down 방향만 설정 가능 (R로 변경)");
                ctx.SetHintVisible?.Invoke(true);
                return;
            }
            ctx.SetHintVisible?.Invoke(false);

            int th = Mathf.Max(1, band.Thickness);

            int clickedLaneIndex = band.IsHorizontal
                ? (cell.y - band.StartCell.y)
                : (cell.x - band.StartCell.x);

            if (clickedLaneIndex < 0 || clickedLaneIndex >= th)
                return;

            ctx.PushUndo?.Invoke();

            // band 기준으로 allowed를 정/역으로 해석해서 저장
            ToggleOneLane(cfg, band, clickedLaneIndex, _allowed);

            cfg.PruneOneWayLanesIfHiddenByRoad();

            cfg.TouchRoad();
            ctx.RefreshPreview?.Invoke();
        }

        // =========================
        // Axis helpers
        // =========================
        static bool IsHorizontal(Dir4 d) => d == Dir4.Right || d == Dir4.Left;
        static bool IsVertical(Dir4 d) => d == Dir4.Up || d == Dir4.Down;

        // =========================
        // Existing OneWay picking
        // =========================
        static bool TryPickExistingOneWayIndexAtCell(LevelConfigViewModel cfg, Vector2Int cell, out int index)
        {
            index = -1;

            if (cfg.OneWayLanes == null || cfg.OneWayLanes.Count == 0)
                return false;

            for (int i = cfg.OneWayLanes.Count - 1; i >= 0; i--)
            {
                var ow = cfg.OneWayLanes[i];

                var start = ow.StartCell.Value;
                var dir = ow.Dir.Value;
                int len = ow.Length.Value;
                int th = ow.Thickness.Value;
                int laneIndex = ow.LaneIndex.Value;

                if (len <= 0 || th <= 0) continue;
                if (laneIndex < 0 || laneIndex >= th) continue;

                if (ContainsCell(start, dir, len, th, laneIndex, cell))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        static bool ContainsCell(Vector2Int start, Dir4 dir, int length, int thickness, int laneIndex, Vector2Int cell)
        {
            bool isH = (dir == Dir4.Right || dir == Dir4.Left);

            if (isH)
            {
                int y = start.y + laneIndex;
                int x0 = start.x;
                int x1 = start.x + (length - 1);

                int minX = Mathf.Min(x0, x1);
                int maxX = Mathf.Max(x0, x1);

                return (cell.y == y) && (cell.x >= minX && cell.x <= maxX);
            }
            else
            {
                int x = start.x + laneIndex;
                int y0 = start.y;
                int y1 = start.y + (length - 1);

                int minY = Mathf.Min(y0, y1);
                int maxY = Mathf.Max(y0, y1);

                return (cell.x == x) && (cell.y >= minY && cell.y <= maxY);
            }
        }

        // =========================
        // Toggle (create/remove)
        // =========================
        static Dir4 ResolveAllowedDir(in RoadBandDetector.BandInfo band, Dir4 toolAllowed)
        {
            var forward = band.Dir;
            var backward = band.Dir.RotateCW().RotateCW();

            if (toolAllowed == forward) return forward;
            if (toolAllowed == backward) return backward;

            // 축이 다르면(원칙상 Tick에서 이미 차단) 방어적으로 backward
            return backward;
        }

        static void ToggleOneLane(LevelConfigViewModel cfg, in RoadBandDetector.BandInfo band, int laneIndex, Dir4 toolAllowed)
        {
            string key = RoadBandDetector.MakeBandKey(band);

            int found = -1;

            for (int i = 0; i < cfg.OneWayLanes.Count; i++)
            {
                var vm = cfg.OneWayLanes[i];
                var b2 = new RoadBandDetector.BandInfo
                {
                    StartCell = vm.StartCell.Value,
                    Dir = vm.Dir.Value,
                    Length = vm.Length.Value,
                    Thickness = vm.Thickness.Value,
                    IsHorizontal = (vm.Dir.Value == Dir4.Right || vm.Dir.Value == Dir4.Left)
                };

                if (RoadBandDetector.MakeBandKey(b2) == key && vm.LaneIndex.Value == laneIndex)
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0)
            {
                cfg.OneWayLanes.RemoveAt(found);
                return;
            }

            var add = new OneWayLaneConfigViewModel();
            add.StartCell.Value = band.StartCell;
            add.Dir.Value = band.Dir;
            add.Length.Value = band.Length;
            add.Thickness.Value = band.Thickness;
            add.LaneIndex.Value = laneIndex;
            add.AllowedDir.Value = ResolveAllowedDir(band, toolAllowed);

            cfg.OneWayLanes.Add(add);
        }
    }
}
