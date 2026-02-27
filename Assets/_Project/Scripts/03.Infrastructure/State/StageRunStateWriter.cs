using System;
using ParkingCat.Entities;
using ParkingCat.Move.Ports;
using ParkingCat.Repository;
using ParkingCat.Sim;
using UnityEngine;


namespace ParkingCat.State
{
    /// <summary>
    /// Commit 반영 표준 구현:
    /// - Car.MarkLeaving()
    /// - StageRun.SpendMoveIfLimited()
    /// - StageRun.RecalcOutcome()
    /// - IStageRunRepository.Save(run)
    /// </summary>
    public sealed class StageRunStateWriter : IStateWriter
    {
        readonly IStageRunRepository _repo;

        public StageRunStateWriter(IStageRunRepository repo) => _repo = repo;

        public void CommitFromSim(int carId, in CarSimState sim, in SimResult result)
        {
            var run = _repo.Get();
            var car = run.Stage.GetCar(carId);

            car.Pos = sim.Pos;
            car.HeadingRad = sim.HeadingRad;
            car.TurnCursor = sim.TurnCursor;

            bool wasLeaving = car.IsLeaving;

            car.MarkLeaving();

            // “이번 커밋으로 인해” 처음 Leaving이 된 경우에만 누적
            if (!wasLeaving && car.IsLeaving)
            {
                int coinDelta = GetEscapeCoinDelta(car); 
                run.AddEscapeReward(coinDelta);
            }

            run.SpendMoveIfLimited();
            run.RecalcOutcome();
            _repo.Save(run);
        }

        static int GetEscapeCoinDelta(Car car)
        {
            if (car == null) return 0;

            if (car.IsCashTruck)
                return Mathf.Max(0, car.CashGold); // 탈출 시 보유분 전액

            return 1; // 기본 차량 1코인
        }

        
        public void ApplyRollbackCost()
        {
            var run = _repo.Get();
            run.SpendMoveIfLimited();
            run.RecalcOutcome();
            _repo.Save(run);
        }
        public void ApplyExtraMoveCost(int extraMoves)
        {
            if (extraMoves <= 0) return;

            var run = _repo.Get();

            // 기본 1 + 추가 페널티
            run.SpendMovesIfLimited(Math.Max(0, extraMoves));

            run.RecalcOutcome();
            _repo.Save(run);
        }

        
        public void ForceExitCar(int carId, bool spendMove)
        {
            var run = _repo.Get();
            var car = run.Stage.GetCar(carId);

            if (car == null) return;
            if (car.IsLeaving) return;

            car.MarkLeaving();

            // 강제 탈출도 “탈출 1회”로 누적
            int coinDelta = GetEscapeCoinDelta(car);
            run.AddEscapeReward(coinDelta);

            if (spendMove)
                run.SpendMoveIfLimited();

            run.RecalcOutcome();
            _repo.Save(run);
        }


        public void ApplyAmbulancePenaltyPerMove()
        {
            var run = _repo.Get();
            if (run == null) return;

            var cars = run.Stage?.Cars;
            if (cars == null || cars.Count == 0) return;

            bool changed = false;
            bool anyFailed = false;

            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i];

                // Leaving(탈출 처리된 차)은 제외
                if (c == null || c.IsLeaving) continue;

                // 구급차만 감소
                if (!c.IsAmbulance) continue;

                // 이미 0 이하이면 실패 상태 유지(추가 감소 방지)
                if (c.RemainingEscapeMoves <= 0)
                {
                    anyFailed = true;
                    continue;
                }

                c.RemainingEscapeMoves -= 1;
                changed = true;

                if (c.RemainingEscapeMoves <= 0)
                    anyFailed = true;
            }

            if (!changed && !anyFailed) return;

            // 실패면 즉시 Fail 
            if (anyFailed)
                run.ForceFail();

            //run.RecalcOutcome();
            _repo.Save(run);
        }
        
        public void AddAmbulanceEscapeMoves(int add)
        {
            if (add <= 0) return;

            var run = _repo.Get();
            if (run == null) return;

            var cars = run.Stage?.Cars;
            if (cars == null || cars.Count == 0) return;

            bool changed = false;

            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                if (c == null || c.IsLeaving) continue;
                if (!c.IsAmbulance) continue;

                // 이미 0 이하였다가 구제되는 케이스 포함
                c.RemainingEscapeMoves = Math.Max(0, c.RemainingEscapeMoves) + add;
                changed = true;
            }

            if (!changed) return;
            run.RecalcOutcome();

            _repo.Save(run);
        }


    }
}