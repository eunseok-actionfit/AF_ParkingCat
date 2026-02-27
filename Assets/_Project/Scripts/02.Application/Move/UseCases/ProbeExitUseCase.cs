using System.Linq;
using ParkingCat.Move.Ports;
using ParkingCat.Sim;

namespace ParkingCat.Move.UseCases
{
    public interface IProbeExitUseCase
    {
        bool WouldExit(int carId);
    }

    public sealed class ProbeExitUseCase : IProbeExitUseCase
    {
        readonly IWorldSnapshotProvider _snap;
        readonly ISimPreRunner _sim;

        public ProbeExitUseCase(IWorldSnapshotProvider snap, ISimPreRunner sim)
        {
            _snap = snap;
            _sim = sim;
        }

        public bool WouldExit(int carId)
        {
            var (sim, world) = _snap.Capture(carId);
            var r = _sim.Run(ref sim, world);

            // “탈출 가능” 정의: 시뮬 결과가 Commit이고 Exit 이벤트가 포함됨
            return r.Decision == SimDecision.Commit
                   && r.Events.Any(e => e.Type == SimEventType.Exit);
        }
    }
}