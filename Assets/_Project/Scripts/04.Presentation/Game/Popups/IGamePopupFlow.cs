using Cysharp.Threading.Tasks;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.Game
{
    public interface IGamePopupFlow
    {
        UniTask ShowStageClearAsync(int levelId, LevelDifficulty difficulty);
        UniTask ShowStageFailedAsync(StageFailReason reason);

        UniTask ShowBuyHintAsync();
        UniTask ShowBuyUFOAsync();
        
        UniTask ShowSettingsAsync(int levelId);
        UniTask ShowExplanationAsync(ExplanationType type);

    }
}