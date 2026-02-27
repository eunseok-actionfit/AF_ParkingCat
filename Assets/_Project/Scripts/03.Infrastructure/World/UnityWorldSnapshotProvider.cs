using System;
using ParkingCat.Entities;
using ParkingCat.Move.Ports;
using ParkingCat.Repository;
using ParkingCat.Sim;


namespace ParkingCat.World
{
    public sealed class UnityWorldSnapshotProvider : IWorldSnapshotProvider
    {
        readonly IStageRunRepository _runRepo;
        readonly ICarRuntimeRegistry _registry;
        readonly IWorldQuery _world;

        public UnityWorldSnapshotProvider(IStageRunRepository runRepo, ICarRuntimeRegistry registry, IWorldQuery world)
        {
            _runRepo = runRepo;
            _registry = registry;
            _world = world;
        }

        public (CarSimState sim, IWorldQuery world) Capture(int carId)
        {
            var run = _runRepo.Get();

            Car target = null;
            foreach (var c in run.Stage.Cars)
                if (c.Id == carId) { target = c; break; }

            if (target == null)
                throw new Exception($"Car not found. carId={carId}");

            var sim = CarSimState.FromDomain(target);

            // Unity 의존 치수 보강 (OK, sim만 변경)
            if (_registry.TryGet(carId, out var info))
                sim.Dims = info.Dims;

            return (sim, _world);
        }

    }
}