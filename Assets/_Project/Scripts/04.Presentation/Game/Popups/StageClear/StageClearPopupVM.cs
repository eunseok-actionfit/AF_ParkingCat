using AES.Tools;
using AES.Tools.UI.Popups;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat.Feature;
using ParkingCat.UseCase.LevelClear;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.Game
{
    public sealed class StageClearPopupVM : PopupViewModelBase
    {
        public AsyncCommand ClaimCommand { get; }

        
        public ObservableProperty<int> RewardCoin = new ();

        ClaimStagePendingCoinUseCase m_claimStagePendingCoinUseCase;


        public StageClearPopupVM(
            SceneFlow.SceneFlow sceneFlow,
            HandleLevelClearedUseCase handleLevelClearedUseCase,
            int levelId,
            LevelDifficulty difficulty,
            ClaimStagePendingCoinUseCase claimStagePendingCoinUseCase)
        {
            m_claimStagePendingCoinUseCase = claimStagePendingCoinUseCase;
            var handleLevelClearedUseCase1 = handleLevelClearedUseCase;
            int levelId1 = levelId;
            var difficulty1 = difficulty;
            RefreshAsync().Forget();

            ClaimCommand = new AsyncCommand(async _ =>
            {
                await handleLevelClearedUseCase1.ExecuteAsync(levelId1, difficulty1);
                await ADS.ShowInterstitialAsync(AdReason.StageClear);

                if (levelId < FirstSceneFeature.NoobMaxLevel)
                    await sceneFlow.GoInGameAsync(levelId + 1);
                else
                    await sceneFlow.GoLobbyAsync();
            });
        }

        async UniTaskVoid RefreshAsync()
        {
            RewardCoin.Value = await m_claimStagePendingCoinUseCase.ExecuteAsync();
        }
    }
}