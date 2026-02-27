using Cysharp.Threading.Tasks;
using ParkingCat.Repository;    
using ParkingCat.Stage.DTOs;

namespace ParkingCat.Stage.UseCases
{
    public sealed class GetStageMetaUseCase
    {
        readonly IStageConfigRepository _cfgRepo;

        public GetStageMetaUseCase(IStageConfigRepository cfgRepo)
        {
            _cfgRepo = cfgRepo;
        }

        public async UniTask<StageMetaDto> ExecuteAsync(int stageId)
        {
            var cfg = await _cfgRepo.LoadAsync(stageId);
            return new StageMetaDto
            {
                StageId = stageId,
                Difficulty = cfg.Difficulty,
            };
        }
    }
}