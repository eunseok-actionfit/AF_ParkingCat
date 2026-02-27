using ParkingCat.Move.Ports;
using ParkingCat.World;


namespace ParkingCat.Sim
{
    /// <summary>
    /// 도메인 시뮬레이터를 애플리케이션 포트에 연결한다.<br/>
    /// 사전 실행 시뮬레이션을 위임한다.
    /// </summary>
    /// <remarks>
    /// 실제 로직은 `SimulatorCore`에만 존재한다.<br/>
    /// 이 클래스는 호출 경로 분리를 위한 어댑터다.
    /// </remarks>
    public sealed class SimPreRunner : ISimPreRunner
    {
        // 실제 시뮬레이션 코어
        readonly SimulatorCore _core;

        /// <summary>
        /// 시뮬레이션 코어를 주입한다.
        /// </summary>
        /// <param name="core">도메인 시뮬레이터</param>
        public SimPreRunner(SimulatorCore core) => _core = core;

        /// <summary>
        /// 차량과 월드 스냅샷으로 시뮬레이션을 실행한다.
        /// </summary>
        /// <param name="car">차량 상태 스냅샷</param>
        /// <param name="world">월드 질의 스냅샷</param>
        /// <returns>시뮬레이션 결과</returns>
        public SimResult Run(ref CarSimState sim, IWorldQuery world)
            => _core.Run(ref sim, world);
    }
}