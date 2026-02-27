using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.Sim;
using ParkingCat.Stage.DTOs;


namespace ParkingCat.Game.Facades
{
    public interface IGameFacade
    {
        UniTask<StageRunDto> StartStageAsync(int stageId);
        StageRunDto GetStageRun();
        
        SimResult TryMove(int moverCarId);
        
        bool HasPreviewStageRun { get; }
        
        void SetPreviewStageRun(StageRunDto dto);
        void ClearPreviewStageRun();
        bool WouldExit(int carId);
        UniTask<bool> TryConsumeBooster(BoosterId id);
    }
}