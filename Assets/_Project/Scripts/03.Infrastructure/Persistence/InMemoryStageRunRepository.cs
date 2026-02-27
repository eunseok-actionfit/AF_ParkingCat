using System;
using ParkingCat.Entities;
using ParkingCat.Repository;


namespace ParkingCat.Persistence
{
    public sealed class InMemoryStageRunRepository : IStageRunRepository
    {
        StageRun? _current;

        public StageRun Get()
        {
            if (_current == null)
                throw new InvalidOperationException("StageRun is not initialized. Call Reset(run) first.");
            return _current;
        }

        public void Reset(StageRun run) => _current = run ?? throw new ArgumentNullException(nameof(run));
        public void Save(StageRun run)  => _current = run ?? throw new ArgumentNullException(nameof(run));
    }
}