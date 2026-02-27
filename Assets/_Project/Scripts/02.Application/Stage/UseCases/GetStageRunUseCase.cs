using ParkingCat.Repository;
using ParkingCat.Stage.DTOs;
using ParkingCat.Stage.Mapping;


namespace ParkingCat.Stage.UseCases
{
    public sealed class GetStageRunUseCase : IGetStageRunUseCase
    {
        readonly IStageRunRepository _repo;

        public GetStageRunUseCase(IStageRunRepository repo)
        {
            _repo = repo;
        }

        public StageRunDto Execute()
        {
            var run = _repo.Get();
            return StageRunMapper.ToDto(run);
        }
    }
}