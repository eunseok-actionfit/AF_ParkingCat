using ParkingCat.Move.Ports;
using ParkingCat.Sim;


namespace ParkingCat.Move.UseCases
{
    /// <summary>
    /// 지정 차량의 이동을 시도하고 결과를 반환한다.<br/>
    /// 입력 잠금과 커밋 적용을 함께 처리한다.
    /// </summary>
    /// <remarks>
    /// 입력이 잠겨 있으면 즉시 Rejected로 종료한다.<br/>
    /// Commit일 때만 상태 반영을 수행한다.
    /// </remarks>
    public sealed class TryMoveUseCase : ITryMoveUseCase
    {
        // 실행 시점의 월드/차량 스냅샷 제공자
        readonly IWorldSnapshotProvider _snap;

        // 시뮬레이션 사전 실행기
        readonly ISimPreRunner _sim;

        // 입력 가능 여부 게이트
        readonly IInputGate _gate;

        // 커밋 결과를 상태에 반영하는 writer
        readonly IStateWriter _state;

        /// <summary>
        /// 유스케이스 실행에 필요한 의존성을 주입한다.
        /// </summary>
        /// <param name="snap">스냅샷 제공자</param>
        /// <param name="sim">시뮬레이션 실행기</param>
        /// <param name="gate">입력 게이트</param>
        /// <param name="state">상태 writer</param>
        public TryMoveUseCase(IWorldSnapshotProvider snap, ISimPreRunner sim, IInputGate gate, IStateWriter state)
        {
            _snap = snap;
            _sim = sim;
            _gate = gate;
            _state = state;
        }

        /// <summary>
        /// 지정 차량에 대해 이동 시뮬레이션을 실행한다.<br/>
        /// Commit일 때만 실제 상태 반영을 수행한다.
        /// </summary>
        /// <param name="carId">대상 차량 ID</param>
        /// <returns>시뮬레이션 결과</returns>
        public SimResult Execute(int carId)
        {
            if (_gate.Locked)
                return new SimResult { Decision = SimDecision.Rejected, Reason = "InputLocked" };

            var (sim, world) = _snap.Capture(carId);
            var result = _sim.Run(ref sim, world);

            if (result.Decision == SimDecision.Commit)
            {
                _state.CommitFromSim(carId, sim, result);
            }
            else if (result.Decision == SimDecision.Rollback)
            {
                _state.ApplyRollbackCost();
            }

            _state.ApplyAmbulancePenaltyPerMove();

            return result;
        }

    }
}