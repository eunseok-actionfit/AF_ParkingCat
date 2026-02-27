using Cysharp.Threading.Tasks;
using ParkingCat.Factories;
using ParkingCat.Repository;
using ParkingCat.Stage.DTOs;
using ParkingCat.Stage.Mapping;


namespace ParkingCat.Stage.UseCases
{
    public sealed class StartStageRunUseCase : IStartStageRunUseCase
    {
        readonly IStageConfigRepository _cfgRepo;
        readonly IStageRunRepository _runRepo;

        public StartStageRunUseCase(IStageConfigRepository cfgRepo, IStageRunRepository runRepo)
        {
            _cfgRepo = cfgRepo;
            _runRepo = runRepo;
        }

        public async UniTask<StageRunDto> ExecuteAsync(int stageId)
        {
            var cfg = await _cfgRepo.LoadAsync(stageId);
            var run = StageFactory.CreateFromConfig(cfg);

            _runRepo.Reset(run);

            return StageRunMapper.ToDto(run);
        }
    }
}