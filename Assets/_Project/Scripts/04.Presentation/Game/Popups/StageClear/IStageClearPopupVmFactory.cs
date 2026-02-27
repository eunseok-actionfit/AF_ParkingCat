using ParkingCat.UseCase.LevelClear;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.Game
{
    public interface IStageClearPopupVmFactory
    {
        StageClearPopupVM Create(int levelId, LevelDifficulty difficulty );
    }

    public sealed class StageClearPopupVmFactory : IStageClearPopupVmFactory
    {
        readonly SceneFlow.SceneFlow _sceneFlow;
        readonly HandleLevelClearedUseCase _useCase;
        readonly ClaimStagePendingCoinUseCase _claimStagePendingCoinUseCase;

        public StageClearPopupVmFactory(SceneFlow.SceneFlow sceneFlow, HandleLevelClearedUseCase useCase, ClaimStagePendingCoinUseCase claimStagePendingCoinUseCase)
        {
            _sceneFlow = sceneFlow;
            _useCase = useCase;
            _claimStagePendingCoinUseCase = claimStagePendingCoinUseCase;
        }

        public StageClearPopupVM Create(int levelId, LevelDifficulty difficulty )
            => new StageClearPopupVM(_sceneFlow, _useCase, levelId, difficulty, _claimStagePendingCoinUseCase);
    }
}