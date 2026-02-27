using System.Collections.Generic;
using UnityEngine;


namespace ParkingCat.Entities
{
    public enum StageStatus { Playing, Cleared, Failed }

    /// <summary>
    /// 스테이지 “진행 상태” (moveLimit, outcome 등).
    /// </summary>
    public sealed class StageRun
    {
        public Stage Stage { get; }
        public int RemainingMoves { get; private set; } // -1이면 무한
        public StageStatus Status { get; private set; } = StageStatus.Playing;

        readonly List<TrafficLightRuntime> _trafficLights = new();
        public IReadOnlyList<TrafficLightRuntime> TrafficLights => _trafficLights;
        
        public int EscapedCount { get; private set; }
        public int PendingCoin { get; private set; }



        public StageRun(Stage stage, int remainingMoves)
        {
            Stage = stage;
            RemainingMoves = remainingMoves;
        }

        public bool IsMoveLimited => RemainingMoves >= 0;

        public void SpendMoveIfLimited()
        {
            if (RemainingMoves < 0) return;
            if (RemainingMoves > 0) RemainingMoves--;
        }
        
        public void SpendMovesIfLimited(int amount)
        {
            if (amount <= 0) return;
            if (RemainingMoves < 0) return;

            RemainingMoves = Mathf.Max(0, RemainingMoves - amount);
        }

        public void RecalcOutcome()
        {
            // 1) 클리어 우선
            if (Stage.ActiveCarCount == 0)
            {
                Status = StageStatus.Cleared;
                return;
            }

            // 2) 앰뷸 실패(기믹 실패)
            for (int i = 0; i < Stage.Cars.Count; i++)
            {
                var c = Stage.Cars[i];
                if (c == null || c.IsLeaving) continue;
                if (!c.IsAmbulance) continue;

                if (c.RemainingEscapeMoves <= 0)
                {
                    Status = StageStatus.Failed;
                    return;
                }
            }

            // 3) 이동횟수 실패(리소스 실패)
            if (RemainingMoves == 0)
            {
                Status = StageStatus.Failed;
                return;
            }

            Status = StageStatus.Playing;

        }
        
        public void ForceFail() => Status = StageStatus.Failed;

        public void AddMoves(int amount)
        {
            if (amount <= 0) return;
            if (RemainingMoves < 0) return;
            RemainingMoves += amount;
        }

        public void InitTrafficLightsFromStage()
        {
            _trafficLights.Clear();
            var s = Stage;

            for (int i = 0; i < s.TrafficLights.Count; i++)
            {
                var t = s.TrafficLights[i];

                _trafficLights.Add(new TrafficLightRuntime(
                    t.Id, t.WorldPos, t.LineDir, t.PhaseOffsetSeconds));
            }
        }
        
        public void AddEscapeReward(int coinDelta)
        {
            if (coinDelta <= 0) return;

            EscapedCount++;
            PendingCoin += coinDelta;
        }

        public int ClaimPendingCoin()
        {
            var amount = PendingCoin;
            PendingCoin = 0;
            return amount;
        }

    }
}