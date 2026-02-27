using System.Collections.Generic;
using ParkingCat.Entities;
using ParkingCat.Stage.DTOs;
using ParkingCat.ValueObjects;
using UnityEngine;
using VContainer;
using Vector2 = UnityEngine.Vector2;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ParkingCat.World
{
    /// <summary>
    /// Unity 물리와 씬 정보를 사용해 `IWorldQuery`를 구현한다.<br/>
    /// 도로, 벽, 차량 충돌을 런타임에서 질의한다.
    /// </summary>
    /// <remarks>
    /// `LayerMask` 설정이 올바르지 않으면 결과가 왜곡될 수 있다.<br/>
    /// `CarViewRegistry`는 씬 내 차량 뷰 목록을 제공해야 한다.
    /// </remarks>
    public sealed class UnityWorldQuery : MonoBehaviour, IWorldQuery, IOneWaySetter
    {
        [SerializeField]
        [Tooltip("월드 경계 사각형")]
        Rect bounds;

        [SerializeField]
        [Tooltip("벽 판정에 사용할 레이어 마스크")]
        LayerMask wallMask;

        [SerializeField]
        [Tooltip("도로 판정에 사용할 레이어 마스크")]
        LayerMask roadMask;

        // OBB 코너 캐시 배열
        readonly Vector2[] _corners = new Vector2[4];

        readonly Dictionary<Vector2Int, Dir4> _oneWayAllowedByCell = new(1024);

        ICarRuntimeRegistry _registry;

        // -------------------------
        // Gizmos Debug (Runtime Pose Cache)
        // -------------------------
        [Header("Gizmos Debug")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] bool drawBounds = true;
        [SerializeField] bool drawLastMoverObb = true;
        [SerializeField] bool drawOtherCarsObb = true;
        [SerializeField] bool drawWallSamplingPoints = true;
        [SerializeField] bool drawOneWayCells = false;
        [SerializeField] bool drawWallHitNormal = true;

        [Tooltip("Scene에서 선택했을 때만 보이게 하려면 true")]
        [SerializeField] bool gizmosOnlyWhenSelected = true;

        // 마지막 CheckCollision 입력(실제 런타임 포즈) 캐시
        bool _hasLastMover;
        Pose2 _lastMoverPose;
        CarDims _lastMoverDims;
        float _lastMoverSpeed;
        int _lastMoverId;

        // 마지막 벽 충돌 정보(있을 때만)
        bool _hasLastWallHit;
        Vector2 _lastWallClosest;
        Vector2 _lastWallNormal;

        [Inject]
        public void Construct(ICarRuntimeRegistry runtime) => _registry = runtime;

        /// <summary>
        /// 월드 경계를 설정한다.
        /// </summary>
        public void SetBounds(Rect r) => bounds = r;

        /// <summary>
        /// 지정 포즈가 월드 경계 안에 있는지 확인한다.
        /// </summary>
        public bool InBounds(in Pose2 pose)
        {
            var p = pose.Pos;
            return bounds.Contains(new Vector2(p.x, p.y));
        }

        /// <summary>
        /// 지정 포즈가 도로 레이어 위에 있는지 확인한다.
        /// </summary>
        public bool IsRoad(in Pose2 pose)
        {
            var p = new Vector2(pose.Pos.x, pose.Pos.y);
            return Physics2D.OverlapPoint(p, roadMask) != null;
        }

        public bool IsEntryAllowed(in Pose2 pose, float headingRad)
        {
            if (!IsRoad(in pose))
                return false;

            var cell = WorldToCell(new Vector2(pose.Pos.x, pose.Pos.y));

            if (_oneWayAllowedByCell.TryGetValue(cell, out var allowed))
            {
                var dir = DirUtil.DirFromHeading(headingRad);
                return dir == allowed;
            }
            
            return true;
        }

        /// <summary>
        /// 이동체의 벽/차량 충돌을 검사하고 연출 파라미터를 반환한다.<br/>
        /// 벽은 점 샘플링으로, 차량은 OBB-SAT로 판정한다.
        /// </summary>
        public CollisionHit CheckCollision(int moverId, in Pose2 moverPose, in CarDims moverDims, float moverSpeed)
        {
            // --- cache for gizmos (실제 충돌 체크에 들어온 포즈) ---
            _hasLastMover = true;
            _lastMoverId = moverId;
            _lastMoverPose = moverPose;
            _lastMoverDims = moverDims;
            _lastMoverSpeed = moverSpeed;
            _hasLastWallHit = false;

            var center = new Vector2(moverPose.Pos.x, moverPose.Pos.y);
            var moverObb = new Obb2D(center, moverPose.HeadingRad, moverDims.Length, moverDims.Width);

            float strength = Mathf.Clamp01(moverSpeed / 6f);

            moverObb.GetCorners(_corners);

            Collider2D wallCol = null;
            for (int i = 0; i < 4 && wallCol == null; i++)
                wallCol = Physics2D.OverlapPoint(_corners[i], wallMask);

            if (wallCol == null)
            {
                for (int i = 0; i < 4 && wallCol == null; i++)
                {
                    var a = _corners[i];
                    var b = _corners[(i + 1) & 3];
                    var mid = (a + b) * 0.5f;
                    wallCol = Physics2D.OverlapPoint(mid, wallMask);
                }
            }

            if (wallCol == null)
            {
                for (int i = 0; i < 4 && wallCol == null; i++)
                {
                    var a = _corners[i];
                    var b = _corners[(i + 1) & 3];
                    var q1 = a + (b - a) * 0.25f;
                    var q3 = a + (b - a) * 0.75f;
                    wallCol = Physics2D.OverlapPoint(q1, wallMask) ?? Physics2D.OverlapPoint(q3, wallMask);
                }
            }

            if (wallCol != null)
            {
                var closest = wallCol.ClosestPoint(center);
                var n = (center - closest);
                var dir = n.sqrMagnitude > 1e-6f ? n.normalized : Vector2.up;

                // cache wall hit gizmo
                _hasLastWallHit = true;
                _lastWallClosest = closest;
                _lastWallNormal = dir;

                return new CollisionHit(
                    hit: true,
                    victimCarId: -1,
                    reason: "Wall",
                    impactDir: new Vector2(dir.x, dir.y),
                    strength: strength);
            }

            foreach (var kv in _registry.All)
            {
                var info = kv;
                if (info.CarId == moverId) continue;

                var posTf = info.PosTf;
                if (posTf == null) continue;
                var rotTf = info.RotTf != null ? info.RotTf : posTf;

                var otherCenter = (Vector2)posTf.position;
                var forward = rotTf.up;
                float heading = Mathf.Atan2(forward.y, forward.x);

                var dims = info.Dims;
                var otherObb = new Obb2D(otherCenter, heading, dims.Length, dims.Width);

                if (Sat2D.Overlap(in moverObb, in otherObb))
                {
                    var d = (center - otherCenter);
                    var dir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.up;

                    return new CollisionHit(
                        hit: true,
                        victimCarId: info.CarId,
                        reason: "Car",
                        impactDir: new Vector2(dir.x, dir.y),
                        strength: strength);
                }
            }

            return CollisionHit.None;
        }

        public void SetOneWay(StageRunDto.OneWayLaneDto[] oneWays)
        {
            _oneWayAllowedByCell.Clear();
            if (oneWays == null) return;

            for (int i = 0; i < oneWays.Length; i++)
            {
                var ow = oneWays[i];
                if (ow == null) continue;
                if (ow.Length <= 0) continue;
                if (ow.Thickness <= 0) continue;
                if (ow.LaneIndex < 0 || ow.LaneIndex >= ow.Thickness) continue;

                bool isH = (ow.Dir == Dir4.Right || ow.Dir == Dir4.Left);

                if (isH)
                {
                    int y = ow.StartCell.y + ow.LaneIndex;
                    for (int dx = 0; dx < ow.Length; dx++)
                    {
                        int x = ow.StartCell.x + dx;
                        _oneWayAllowedByCell[new Vector2Int(x, y)] = ow.AllowedDir;
                    }
                }
                else
                {
                    int x = ow.StartCell.x + ow.LaneIndex;
                    for (int dy = 0; dy < ow.Length; dy++)
                    {
                        int y = ow.StartCell.y + dy;
                        _oneWayAllowedByCell[new Vector2Int(x, y)] = ow.AllowedDir;
                    }
                }
            }
        }


        static Vector2Int WorldToCell(Vector2 p)
            => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        // -------------------------
        // Gizmos
        // -------------------------
        void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            if (gizmosOnlyWhenSelected) return;
            DrawAllGizmos();
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            if (!gizmosOnlyWhenSelected) return;
            DrawAllGizmos();
        }

        void DrawAllGizmos()
        {
            if (drawBounds) DrawBoundsGizmo();

            if (drawLastMoverObb && _hasLastMover)
            {
                var center = new Vector2(_lastMoverPose.Pos.x, _lastMoverPose.Pos.y);
                var obb = new Obb2D(center, _lastMoverPose.HeadingRad, _lastMoverDims.Length, _lastMoverDims.Width);

                // mover OBB
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.85f);
                DrawObb(obb, 0.02f);

#if UNITY_EDITOR
                Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                Handles.Label(new Vector3(center.x, center.y, 0),
                    $"Mover id={_lastMoverId}\nspd={_lastMoverSpeed:0.00}\nheading={(_lastMoverPose.HeadingRad * Mathf.Rad2Deg):0.0}°");
#endif

                if (drawWallSamplingPoints)
                    DrawWallSamplingPoints(obb);

                if (drawWallHitNormal && _hasLastWallHit)
                    DrawWallHit(center);
            }

            if (drawOtherCarsObb && _registry != null)
                DrawRegistryCars();

            if (drawOneWayCells)
                DrawOneWayCells();
        }

        void DrawBoundsGizmo()
        {
            var center = new Vector3(bounds.center.x, bounds.center.y, 0);
            var size = new Vector3(bounds.size.x, bounds.size.y, 0);

            Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
            Gizmos.DrawWireCube(center, size);
        }

        void DrawRegistryCars()
        {
            foreach (var info in _registry.All)
            {
                var posTf = info.PosTf;
                if (posTf == null) continue;
                var rotTf = info.RotTf != null ? info.RotTf : posTf;

                var c = (Vector2)posTf.position;

                // Query 로직과 동일하게 rotTf.up 기반 heading 계산
                var f = (Vector2)rotTf.up;
                float heading = Mathf.Atan2(f.y, f.x);

                var d = info.Dims;
                var obb = new Obb2D(c, heading, d.Length, d.Width);

                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                DrawObb(obb, 0.01f);

                // forward 표시
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f);
                Gizmos.DrawLine(new Vector3(c.x, c.y, 0), new Vector3(c.x + f.x * 0.6f, c.y + f.y * 0.6f, 0));
            }
        }

        void DrawOneWayCells()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);

            foreach (var kv in _oneWayAllowedByCell)
            {
                var cell = kv.Key;
                var center = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0);
                Gizmos.DrawWireCube(center, new Vector3(1f, 1f, 0));

#if UNITY_EDITOR
                Handles.color = new Color(0.2f, 1f, 0.2f, 0.9f);
                Handles.Label(center, kv.Value.ToString());
#endif
            }
        }

        void DrawWallHit(Vector2 moverCenter)
        {
            // closest point
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawSphere(new Vector3(_lastWallClosest.x, _lastWallClosest.y, 0), 0.05f);

            // line to closest
            Gizmos.DrawLine(new Vector3(moverCenter.x, moverCenter.y, 0), new Vector3(_lastWallClosest.x, _lastWallClosest.y, 0));

            // normal
            var nEnd = _lastWallClosest + _lastWallNormal * 0.6f;
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
            Gizmos.DrawLine(new Vector3(_lastWallClosest.x, _lastWallClosest.y, 0), new Vector3(nEnd.x, nEnd.y, 0));
        }

        static void DrawObb(in Obb2D obb, float zOffset)
        {
            var corners = new Vector2[4];
            obb.GetCorners(corners);

            var a = new Vector3(corners[0].x, corners[0].y, zOffset);
            var b = new Vector3(corners[1].x, corners[1].y, zOffset);
            var c = new Vector3(corners[2].x, corners[2].y, zOffset);
            var d = new Vector3(corners[3].x, corners[3].y, zOffset);

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        static void DrawWallSamplingPoints(in Obb2D obb)
        {
            var corners = new Vector2[4];
            obb.GetCorners(corners);

            // corners
            Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
            for (int i = 0; i < 4; i++)
                Gizmos.DrawSphere(new Vector3(corners[i].x, corners[i].y, 0), 0.04f);

            // edge mids
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.7f);
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) & 3];
                var mid = (a + b) * 0.5f;
                Gizmos.DrawSphere(new Vector3(mid.x, mid.y, 0), 0.03f);
            }

            // q1/q3
            Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.7f);
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) & 3];
                var q1 = a + (b - a) * 0.25f;
                var q3 = a + (b - a) * 0.75f;
                Gizmos.DrawSphere(new Vector3(q1.x, q1.y, 0), 0.025f);
                Gizmos.DrawSphere(new Vector3(q3.x, q3.y, 0), 0.025f);
            }
        }
    }
}
