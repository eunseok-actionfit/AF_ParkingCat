using System;
using ParkingCat.ValueObjects;
using ParkingCat.World;
using UnityEngine;


namespace ParkingCat.Sim
{
    public sealed class SimulatorCore
    {
        readonly SimulationSettings _cfg;

        public SimulatorCore(SimulationSettings cfg) => _cfg = cfg;

        enum MotionMode { Straight, Turning }

        MotionMode _mode;

        float _turnStartHeading;
        float _turnTargetHeading;
        float _turnSign;
        Vector2 _turnCenter;
        float _turnProgress;

        // 턴 시작 시 반지름 벡터(턴 시작 위치 점프 방지)
        Vector2 _turnStartRadiusVec;

        float _junctionConsumeCooldownDist = 0f;

        // 출발 직후 턴 소비 방지(너무 빨리 꺾이는 문제)
        float _startTurnBlockDist = 0f;

        // =========================================================
        // "현재 주행 차선 중앙" 유지용 상태
        // - 동/서 주행: 횡=Y 고정
        // - 남/북 주행: 횡=X 고정
        // =========================================================
        bool _hasLaneCenter = false;
        bool _laneAxisIsY = true; // true: E/W(횡=Y), false: N/S(횡=X)
        float _laneCenterCoord = 0f; // 목표 횡방향 중앙 (x 또는 y)
        // =========================================================

        public SimResult Run(ref CarSimState car, IWorldQuery world)
        {
            var r = new SimResult();
            var pose = new Pose2(car.Pos, car.HeadingRad);

            if (!world.InBounds(in pose))
            {
                r.Decision = SimDecision.Rejected;
                r.Reason = "StartOutOfBounds";
                return r;
            }

            _mode = MotionMode.Straight;
            _turnProgress = 0f;
            _junctionConsumeCooldownDist = 0f;

            // 출발 직후 일정 거리만큼은 턴 소비 금지
            _startTurnBlockDist = MathF.Max(_cfg.JunctionLookAhead, _cfg.ForwardSpeed * _cfg.Dt * 2f);

            // 시작 시점 차선 중앙 캡쳐
            CaptureLaneCenter(in pose);

            int maxSteps = (int)MathF.Ceiling(_cfg.MaxSeconds / _cfg.Dt);

            r.Forward.Poses.Add(pose);

            for (int step = 0; step < maxSteps; step++)
            {
                var nextPose = StepCandidate(pose);

                if (!world.InBounds(in nextPose))
                {
                    r.Decision = SimDecision.Commit;
                    r.Reason = "LeftBoundary";
                    r.Events.Add(new SimEvent(SimEventType.Exit, r.Forward.Count - 1, TurnAction.Straight, -1, Vector2.zero, 0f, r.Reason));
                    return r;
                }

                if (!world.IsRoad(in nextPose))
                {
                    var fwd = Forward(nextPose.HeadingRad);
                    var impactDir = -fwd;

                    SetCollisionRollback(ref r,
                        victimId: -1,
                        impactDir: impactDir,
                        strength: StrengthFromSpeed(_cfg.ForwardSpeed),
                        reason: "Wall");

                    BuildRollback(r);
                    return r;
                }

                // 원웨이 차단: prev->mid->next 구간으로 잡음
                if (!IsSegmentEntryAllowed(world, in pose, in nextPose))
                {
                    var fwd = Forward(pose.HeadingRad);
                    var impactDir = -fwd;

                    SetCollisionRollback(ref r,
                        victimId: -1,
                        impactDir: impactDir,
                        strength: StrengthFromSpeed(_cfg.ForwardSpeed),
                        reason: "OneWayBlocked");

                    BuildRollback(r);
                    return r;
                }

                var hit = world.CheckCollision(car.CarId, in nextPose, in car.Dims, moverSpeed: _cfg.ForwardSpeed);

                if (hit.Hit)
                {
                    SetCollisionRollback(ref r, hit.VictimCarId, hit.ImpactDir, hit.Strength, hit.Reason);
                    BuildRollback(r);
                    return r;
                }

                var prevPose = pose;
                pose = nextPose;

                float moved = Vector2.Distance(prevPose.Pos, pose.Pos);

                if (_junctionConsumeCooldownDist > 0f)
                    _junctionConsumeCooldownDist = MathF.Max(0f, _junctionConsumeCooldownDist - moved);

                if (_startTurnBlockDist > 0f)
                    _startTurnBlockDist = MathF.Max(0f, _startTurnBlockDist - moved);

                car.Pos = pose.Pos;
                car.HeadingRad = pose.HeadingRad;
                r.Forward.Poses.Add(pose);

                TryHandleJunctionAndTurn_OneLane(ref car, world, in pose, frameIndex: r.Forward.Count - 1, r);
            }

            r.Decision = SimDecision.Rollback;
            r.Reason = "MaxSimTime";
            r.HitFrameIndex = r.Forward.Count - 1;
            r.VictimCarId = -1;
            r.ImpactDir = Vector2.zero;
            r.ImpactStrength = 0f;
            BuildRollback(r);
            return r;
        }

        static void SetCollisionRollback(ref SimResult r, int victimId, Vector2 impactDir, float strength, string reason)
        {
            r.Decision = SimDecision.Rollback;
            r.Reason = reason;

            r.HitFrameIndex = r.Forward.Count - 1;
            r.VictimCarId = victimId;
            r.ImpactDir = impactDir;
            r.ImpactStrength = strength;

            r.Events.Add(new SimEvent(
                SimEventType.Collision,
                r.HitFrameIndex,
                TurnAction.Straight,
                victimId,
                impactDir,
                strength,
                reason));
        }

        void BuildRollback(SimResult r)
        {
            int last = Math.Clamp(r.HitFrameIndex, 0, r.Forward.Count - 1);

            for (int i = last; i >= 0; i--)
                r.Rollback.Poses.Add(r.Forward.Poses[i]);
        }

        Pose2 StepCandidate(Pose2 current)
        {
            float dt = _cfg.Dt;

            if (_mode == MotionMode.Straight)
            {
                Vector2 fwd = Forward(current.HeadingRad);
                var pos = current.Pos + fwd * (_cfg.ForwardSpeed * dt);

                // 직진 구간에서 차선 중앙으로 "횡방향" 보정
                pos = ApplyLaneCentering(pos, dt);

                return new Pose2(pos, current.HeadingRad);
            }

            float arcLen = (MathF.PI * 0.5f) * _cfg.TurnRadius;
            float stepLen = _cfg.TurnSpeed * dt;
            float prog = _turnProgress + stepLen / MathF.Max(0.0001f, arcLen);
            if (prog > 1f) prog = 1f;

            float heading = LerpAngle(_turnStartHeading, _turnTargetHeading, prog);
            float angleDelta = (_turnSign * (MathF.PI * 0.5f)) * prog;

            // prog=0일 때 pos가 현재 위치와 정확히 같도록 "시작 반지름 벡터"를 회전
            Vector2 vec = Rotate(_turnStartRadiusVec, angleDelta);
            Vector2 pos2 = _turnCenter + vec;

            _turnProgress = prog;

            if (_turnProgress >= 1f)
            {
                _mode = MotionMode.Straight;

                // 턴 종료 시, 새 방향 기준 차선 중앙 재캡쳐
                var endPose = new Pose2(pos2, heading);
                CaptureLaneCenter(in endPose);
            }

            return new Pose2(pos2, heading);
        }

        // =========================================================
        // 1차선(타일) 기준 분기 처리
        //
        // - "옆 차선이 열려있음(다차선)"을 분기로 오인해서 TurnProgram이 소비되는 문제 방지
        // - 단일 셀만 보는 대신, 해당 방향으로 '깊이(depthCells)' 만큼 연속으로 열려있는지 확인
        //   => 멀티레인 직선에서의 좌/우(차선 이동)는 보통 1~2셀 내로 끝나므로 분기에서 제외됨
        // =========================================================
        void TryHandleJunctionAndTurn_OneLane(ref CarSimState car, IWorldQuery world, in Pose2 pose, int frameIndex, SimResult outResult)
        {
            if (_junctionConsumeCooldownDist > 0f)
                return;

            if (_startTurnBlockDist > 0f)
                return;

            Vector2 fwd = Forward(pose.HeadingRad);
            Vector2 left = new Vector2(-fwd.y, fwd.x);

            float fwdHeading = pose.HeadingRad;
            float leftHeading = NormalizeAngle(pose.HeadingRad + (MathF.PI * 0.5f));
            float rightHeading = NormalizeAngle(pose.HeadingRad - (MathF.PI * 0.5f));

            Vector2 probePos = pose.Pos + fwd * _cfg.JunctionLookAhead;

            // 기존 depth 체크용 step (타일 기반이면 1이 안전)
            float step = MathF.Max(_cfg.BranchProbe, 1.0f);

            const int TURN_DEPTH_CELLS = 3; // 필요하면 2~4

            // 1) 기존 "깊이로 열린 방향" 판정(원웨이/도로 포함)
            bool forwardOpen = IsBranchOpenDepth(world, probePos, fwd, fwdHeading, step, depthCells: 2);
            bool leftOpen = IsBranchOpenDepth(world, probePos, left, leftHeading, step, depthCells: TURN_DEPTH_CELLS);
            bool rightOpen = IsBranchOpenDepth(world, probePos, -left, rightHeading, step, depthCells: TURN_DEPTH_CELLS);

            // 2) 추가: "폭 증가(새로 열린 가지)"가 있는지 확인
            // - 멀티레인(원래부터 넓은 도로)은 nowReach와 probeReach가 비슷 -> branch=false
            float leftReachNow = MeasureSideReach(world, pose.Pos, left, leftHeading);
            float rightReachNow = MeasureSideReach(world, pose.Pos, -left, rightHeading);

            float leftReachProbe = MeasureSideReach(world, probePos, left, leftHeading);
            float rightReachProbe = MeasureSideReach(world, probePos, -left, rightHeading);

            bool leftBranch = leftOpen && (leftReachProbe - leftReachNow) > _cfg.BranchDeltaThreshold;
            bool rightBranch = rightOpen && (rightReachProbe - rightReachNow) > _cfg.BranchDeltaThreshold;

            // "교차로/분기"라고 부를만한 조건:
            // - 전방이 열려있고(보통), 좌/우 중 하나라도 "새로 열린 가지"가 있을 때
            // - 혹은 전방이 막히고 좌/우로만 새로 열리는 상황(T자 막다른 교차로)도 처리
            int openCount = (forwardOpen ? 1 : 0) + (leftBranch ? 1 : 0) + (rightBranch ? 1 : 0);

            bool isCorner = !forwardOpen && (leftOpen ^ rightOpen);
            if (openCount < 1 && !isCorner)
                return;

            var action = car.TurnProgram.Peek(car.TurnCursor);

            // 3) 토큰 소비 조건 강화
            // - Left/Right는 branch일 때만
            // - Straight는 "branch가 존재하는 진짜 교차로"일 때만(폭 변화 없는 멀티레인 직선에서 소비 방지)
            bool canConsume = action switch
            {
                TurnAction.Left => leftBranch,
                TurnAction.Right => rightBranch,
                TurnAction.Straight => forwardOpen && (leftBranch || rightBranch),
                _ => false
            };

            if (!canConsume)
                return;

            car.TurnCursor = car.TurnProgram.Consume(car.TurnCursor);

            outResult.Events.Add(new SimEvent(
                SimEventType.JunctionTurnConsumed,
                frameIndex,
                action,
                -1,
                Vector2.zero,
                0f,
                "Junction"));

            if (_mode != MotionMode.Turning)
            {
                if (action == TurnAction.Left) BeginTurn(pose, turnLeft: true);
                else if (action == TurnAction.Right) BeginTurn(pose, turnLeft: false);
            }

            _junctionConsumeCooldownDist = MathF.Max(_cfg.JunctionConsumeCooldownDist, _cfg.ForwardSpeed * _cfg.Dt * 1.1f);
        }


        //  방향으로 depthCells 만큼 연속으로 갈 수 있는지 체크
        // - IsEntryAllowed 포함 (원웨이 반영)
        static bool IsBranchOpenDepth(IWorldQuery world, Vector2 probePos, Vector2 dirUnit, float moveHeading, float step, int depthCells)
        {
            if (depthCells < 1) depthCells = 1;

            // 첫 셀 포함해서 depthCells 만큼
            for (int i = 1; i <= depthCells; i++)
            {
                var p = probePos + dirUnit * (step * i);
                var s = new Pose2(p, moveHeading);

                if (!world.InBounds(in s)) return false;
                if (!world.IsRoad(in s)) return false;
                if (!world.IsEntryAllowed(in s, moveHeading)) return false;
            }

            return true;
        }

// 횡방향(sideDirUnit)으로 "연속해서 갈 수 있는" 거리(월드 유닛)를 측정
// - InBounds / IsRoad / IsEntryAllowed 모두 만족해야 카운트
// - SideSampleStep, SideMaxReach, BranchDeltaThreshold 설정을 그대로 사용
        float MeasureSideReach(IWorldQuery world, Vector2 origin, Vector2 sideDirUnit, float moveHeading)
        {
            float step = MathF.Max(0.01f, _cfg.SideSampleStep);
            float max = MathF.Max(step, _cfg.SideMaxReach);

            float reach = 0f;

            // 0은 origin 자체라서 보통 road임. 실제 확장은 1스텝부터
            for (float d = step; d <= max + 1e-6f; d += step)
            {
                var p = origin + sideDirUnit * d;
                var s = new Pose2(p, moveHeading);

                if (!world.InBounds(in s)) break;
                if (!world.IsRoad(in s)) break;
                if (!world.IsEntryAllowed(in s, moveHeading)) break;

                reach = d;
            }

            return reach;
        }

        // prev->mid->next 를 이동방향 헤딩으로 원웨이 체크
        static bool IsSegmentEntryAllowed(IWorldQuery world, in Pose2 prev, in Pose2 next)
        {
            Vector2 delta = next.Pos - prev.Pos;

            if (delta.sqrMagnitude < 1e-8f)
                return world.IsEntryAllowed(in next, next.HeadingRad);

            float moveHeading = MathF.Atan2(delta.y, delta.x);

            var midPos = prev.Pos + delta * 0.5f;
            var mid = new Pose2(midPos, moveHeading);

            if (world.IsRoad(in mid) && !world.IsEntryAllowed(in mid, moveHeading))
                return false;

            var end = new Pose2(next.Pos, moveHeading);

            if (world.IsRoad(in end) && !world.IsEntryAllowed(in end, moveHeading))
                return false;

            return true;
        }

        void BeginTurn(in Pose2 pose, bool turnLeft)
        {
            _mode = MotionMode.Turning;
            _turnProgress = 0f;

            _turnStartHeading = pose.HeadingRad;
            _turnSign = turnLeft ? +1f : -1f;
            _turnTargetHeading = NormalizeAngle(_turnStartHeading + _turnSign * (MathF.PI * 0.5f));

            Vector2 fwd = Forward(_turnStartHeading);
            Vector2 left = new Vector2(-fwd.y, fwd.x);
            Vector2 side = turnLeft ? left : -left;

            _turnCenter = pose.Pos + side * _cfg.TurnRadius;

            // 턴 시작 위치가 튀지 않도록 시작 반지름 벡터 저장
            _turnStartRadiusVec = pose.Pos - _turnCenter;
        }

        // =========================================================
        // Lane center helpers
        // =========================================================
        static int CellOf(float v) => (int)MathF.Floor(v);
        static float CellCenter(int cell) => cell + 0.5f;

        void CaptureLaneCenter(in Pose2 pose)
        {
            float a = NormalizeAngle(pose.HeadingRad);
            float ax = MathF.Abs(MathF.Cos(a));
            float ay = MathF.Abs(MathF.Sin(a));

            // East/West이면 횡방향은 Y, North/South이면 횡방향은 X
            _laneAxisIsY = ax >= ay;

            if (_laneAxisIsY)
            {
                int laneCell = CellOf(pose.Pos.y);
                _laneCenterCoord = CellCenter(laneCell);
            }
            else
            {
                int laneCell = CellOf(pose.Pos.x);
                _laneCenterCoord = CellCenter(laneCell);
            }

            _hasLaneCenter = true;
        }

        Vector2 ApplyLaneCentering(Vector2 pos, float dt)
        {
            if (!_hasLaneCenter) return pos;

            // 횡 보정 속도(월드 유닛/초): 너무 크면 스냅처럼 보임
            float maxLatSpeed = _cfg.ForwardSpeed * 2.0f;

            if (_laneAxisIsY)
            {
                float err = _laneCenterCoord - pos.y;
                float step = Math.Clamp(err, -maxLatSpeed * dt, maxLatSpeed * dt);
                pos.y += step;
            }
            else
            {
                float err = _laneCenterCoord - pos.x;
                float step = Math.Clamp(err, -maxLatSpeed * dt, maxLatSpeed * dt);
                pos.x += step;
            }

            return pos;
        }
        // =========================================================

        float StrengthFromSpeed(float speed)
            => Math.Clamp(speed / MathF.Max(0.001f, _cfg.ImpactSpeedForMax), 0f, 1f);

        static Vector2 Forward(float headingRad) => new(MathF.Cos(headingRad), MathF.Sin(headingRad));

        static float LerpAngle(float a, float b, float t)
        {
            float d = NormalizeAngle(b - a);
            return NormalizeAngle(a + d * t);
        }

        static float NormalizeAngle(float a)
        {
            while (a > MathF.PI) a -= 2f * MathF.PI;
            while (a < -MathF.PI) a += 2f * MathF.PI;
            return a;
        }

        static Vector2 Rotate(Vector2 v, float rad)
        {
            float c = MathF.Cos(rad);
            float s = MathF.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}