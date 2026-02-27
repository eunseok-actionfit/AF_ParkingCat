using System.Threading;
using AES.Tools;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat.Economy;
using ParkingCat.Lobby.Popups;
using ParkingCat.Lobby.Shop;
using ParkingCat.Stage.UseCases;
using ParkingCat.UseCase.Economy;
using ParkingCat.UseCase.Economy.Lives;
using ParkingCat.UseCase.Home;
using ParkingCat.ValueObjects.Level;
using UnityEngine;


namespace ParkingCat.Lobby
{
    public sealed class LobbySceneViewModel
    {
        public ObservableProperty<int> StageNumber { get; } = new(1);
        public ObservableProperty<LevelDifficulty> CurrentDifficulty { get; } = new();
        
        public ObservableProperty<int> NextStageNumber { get; } = new(1);
        public ObservableProperty<LevelDifficulty> NextDifficulty { get; } = new();

        public AsyncCommand StartGameCommand { get; }
        public AsyncCommand OpenSettingsCommand { get; }
        public AsyncCommand OpenHeartRefillCommand { get; }
        public AsyncCommand OpenAdsRemoveCommand { get; }
        
        public ObservableProperty<bool> IsAdsRemoved { get; } = new();


        [Bindable]
        public ShopViewModel ShopVm { get; }

        [Bindable]
        public LivesViewModel LivesVm { get; }

        [Bindable]
        public CoinViewModel CoinVM { get; }

        readonly SceneFlow.SceneFlow _flow;


        readonly GetHomeDefaultStageUseCase _getDefaultStageUseCase;
        readonly CanStartStageUseCase _canStartStageUseCase;
        readonly ConsumeLifeForStageUseCase _consumeLifeForStageUseCase;
        readonly GetStageMetaUseCase _getStageMetaUseCase;
        readonly GetAdsRemoveUseCase _getAdsRemoveUseCase;


        bool _lastCanStartResult = false;


        public LobbySceneViewModel(
            SceneFlow.SceneFlow flow,
            ILobbyPopupFlow lobbyPopups,
            GetHomeDefaultStageUseCase getDefaultStageUseCase,
            CanStartStageUseCase canStartStageUseCase,
            ConsumeLifeForStageUseCase consumeLifeForStageUseCase,
            LivesViewModel livesVm,
            CoinViewModel coinVM,
            ShopViewModel shopVm,
            GetStageMetaUseCase getStageMetaUseCase,
            GetAdsRemoveUseCase getAdsRemoveUseCase)
        {

            _flow = flow;
            ShopVm = shopVm;
            _getStageMetaUseCase = getStageMetaUseCase;
            _getAdsRemoveUseCase = getAdsRemoveUseCase;
            LivesVm = livesVm;
            CoinVM = coinVM;
            
            _getDefaultStageUseCase = getDefaultStageUseCase;
            _canStartStageUseCase = canStartStageUseCase;
            _consumeLifeForStageUseCase = consumeLifeForStageUseCase;

            StartGameCommand = new AsyncCommand(
                _ => StartGameAsync(),
                _ => _lastCanStartResult);
            

            OpenSettingsCommand = new AsyncCommand(_ => lobbyPopups.ShowSettingsAsync());
            OpenHeartRefillCommand = new AsyncCommand(_ => lobbyPopups.ShowHeartRefillAsync());
            OpenAdsRemoveCommand = new AsyncCommand(_ => lobbyPopups.ShowAdsRemoveAsync());

            IAP.PurchaseConfirmedByProductKey += _ =>
            {
                CoinVM.RefreshAsync().Forget();
                LivesVm.RefreshOnceAsync().Forget();
                RefreshAdsRemovedStatusAsync().Forget();
            };

            Initialize().Forget();
        }

        async UniTaskVoid Initialize()
        {
            var defaultStage = await _getDefaultStageUseCase.ExecuteAsync();
            StageNumber.Value = defaultStage;
            NextStageNumber.Value = defaultStage + 1;

            // 초기 난이도 로드
            await RefreshDifficultyAsync(StageNumber.Value);

            _lastCanStartResult = await _canStartStageUseCase.CanStartAsync(StageNumber.Value);

            StageNumber.OnValueChanged += async newValue =>
            {
                _lastCanStartResult = await _canStartStageUseCase.CanStartAsync(newValue);
                await RefreshDifficultyAsync(newValue);
            };

            IsAdsRemoved.Value = ADS.IsRuntimeDisabled;
        }
        async UniTaskVoid RefreshAdsRemovedStatusAsync()
        {
            IsAdsRemoved.Value = await _getAdsRemoveUseCase.ExecuteAsync();
        }

        async UniTask RefreshDifficultyAsync(int stageId)
        {
            var meta = await _getStageMetaUseCase.ExecuteAsync(stageId);
            CurrentDifficulty.Value = meta.Difficulty;
            var nextMeta = await _getStageMetaUseCase.ExecuteAsync(stageId + 1);
            NextDifficulty.Value = nextMeta.Difficulty;
        }


        async UniTask StartGameAsync()
        {
            
            var success = await _consumeLifeForStageUseCase.ExecuteAsync();
            if (!success)
            {
                await OpenHeartRefillCommand.ExecuteAsync();
                return;
            }

            var stage = StageNumber.Value;
            await _flow.GoInGameAsync(stage, CancellationToken.None);
        }

    }
}