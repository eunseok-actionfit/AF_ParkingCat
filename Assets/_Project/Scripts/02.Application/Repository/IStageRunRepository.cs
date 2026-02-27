

using ParkingCat.Entities;


namespace ParkingCat.Repository
{
    public interface IStageRunRepository
    {
        StageRun Get();
        void Reset(StageRun run);
        void Save(StageRun run);
    }
}