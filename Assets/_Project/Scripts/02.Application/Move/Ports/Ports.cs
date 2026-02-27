using ParkingCat.Sim;
using ParkingCat.World;


namespace ParkingCat.Move.Ports
{
    /// <summary>
    /// 시뮬레이션 실행을 위한 월드와 차량 스냅샷을 제공한다.<br/>
    /// 실행 중 원본 상태가 변하지 않도록 분리한다.
    /// </summary>
    public interface IWorldSnapshotProvider
    {
        /// <summary>
        /// 지정 차량 기준의 스냅샷을 캡처한다.
        /// </summary>
        /// <param name="carId">대상 차량 ID</param>
        /// <returns>차량 상태와 월드 질의 쌍</returns>
        (CarSimState sim, IWorldQuery world) Capture(int carId);
    }

    /// <summary>
    /// 입력 허용 여부를 제어하는 게이트를 정의한다.<br/>
    /// 중복 입력이나 동시 실행을 방지한다.
    /// </summary>
    public interface IInputGate
    {
        /// <summary>
        /// 입력이 잠겨 있는지 여부를 반환한다.
        /// </summary>
        bool Locked { get; }

        /// <summary>
        /// 입력을 잠금 상태로 전환한다.
        /// </summary>
        void Lock();

        /// <summary>
        /// 입력 잠금을 해제한다.
        /// </summary>
        void Unlock();
    }

    /// <summary>
    /// 시뮬레이션 결과를 실제 상태에 반영한다.<br/>
    /// Commit 결정에 대해서만 호출된다.
    /// </summary>
    public interface IStateWriter
    {

        void CommitFromSim(int carId, in CarSimState sim, in SimResult result);
        void ApplyRollbackCost();
        void ApplyExtraMoveCost(int extraMoves);
        void ForceExitCar(int carId, bool spendMove);
        void ApplyAmbulancePenaltyPerMove();
        void AddAmbulanceEscapeMoves(int add);
    }

    /// <summary>
    /// 시뮬레이션을 사전 실행하는 포트를 정의한다.<br/>
    /// 애플리케이션 계층에서 도메인 실행을 분리한다.
    /// </summary>
    public interface ISimPreRunner
    {
        /// <summary>
        /// 차량과 월드 스냅샷으로 시뮬레이션을 실행한다.
        /// </summary>
        /// <param name="car">차량 상태 스냅샷</param>
        /// <param name="world">월드 질의 스냅샷</param>
        /// <returns>시뮬레이션 결과</returns>
        SimResult Run(ref CarSimState sim, IWorldQuery world);

    }
}