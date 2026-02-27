using Cysharp.Threading.Tasks;
using ParkingCat.Stage.DTOs;


namespace ParkingCat.Stage.UseCases
{
    public interface IStartStageRunUseCase
    {
        UniTask<StageRunDto> ExecuteAsync(int stageId);
    }
}