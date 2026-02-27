using UnityEngine;

namespace ParkingCat
{
    public sealed class GameProgressUseCase : IGameEvents
    {
        readonly StageRules _rules;

        int _remainingCars;
        int _movesUsed;
        RunStatus _status;

        public RunStatus Status => _status;
        public int RemainingCars => _remainingCars;
        public int MovesUsed => _movesUsed;

        public GameProgressUseCase(StageRules rules, int initialCars)
        {
            _rules = rules;
            _remainingCars = Mathf.Max(0, initialCars);
            _movesUsed = 0;
            _status = RunStatus.Running;

            Debug.Log($"[GAME] Start Cars={_remainingCars} MoveLimit={(rules.HasMoveLimit ? rules.MoveLimit.ToString() : "∞")}");
        }

        // public void OnMoveStarted(int carId, PlanType type)
        // {
        //     if (_status != RunStatus.Running) return;
        //
        //     // Move 제한이 없으면 카운트 자체를 안 올려도 되지만,
        //     // 디버그/통계용으로 올리는 건 괜찮음. (원치 않으면 rules.HasMoveLimit일 때만++)
        //     _movesUsed++;
        //
        //     Debug.Log($"[GAME] MoveStarted Car={carId} Type={type} Moves={_movesUsed}/{(_rules.HasMoveLimit ? _rules.MoveLimit.ToString() : "∞")}");
        //
        //     if (_rules.HasMoveLimit && _movesUsed > _rules.MoveLimit)
        //     {
        //         Lose($"MoveLimit exceeded: {_movesUsed}>{_rules.MoveLimit}");
        //     }
        // }

        public void OnCarExited(int carId)
        {
            if (_status != RunStatus.Running) return;

            _remainingCars = Mathf.Max(0, _remainingCars - 1);
            Debug.Log($"[GAME] CarExited Car={carId} RemainingCars={_remainingCars}");

            if (_remainingCars == 0)
            {
                Win();
            }
        }

        public void OnMoveRejected(int carId, string reason)
        {
            if (_status != RunStatus.Running) return;
            Debug.Log($"[GAME] MoveRejected Car={carId} Reason={reason}");
        }

        public void OnFailTriggered(string reason)
        {
            if (_status != RunStatus.Running) return;
            Lose(reason);
        }

        void Win()
        {
            _status = RunStatus.Win;
            Debug.Log("[GAME] WIN");
        }

        void Lose(string reason)
        {
            _status = RunStatus.Lose;
            Debug.Log($"[GAME] LOSE Reason=\"{reason}\"");
        }
    }
}
