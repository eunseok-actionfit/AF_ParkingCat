using AES.Tools;
using AES.Tools.UI.Popups;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat.Economy;
using ParkingCat.UseCase.Home;


namespace ParkingCat.Lobby.Popups.HeartRefill
{
    public sealed class HeartRefillPopupVM : PopupViewModelBase
    {
        public Command CloseCommand { get; }
        
        
        public Command RefillLivesWithCoinCommand { get; }
        
        public AsyncCommand WatchRewardAdForLifeCommand { get; }

        readonly RefillLivesWithCoinUseCase _refillLivesWithCoinUseCase;
        readonly AddLifeFromRewardUseCase   _addLifeFromRewardUseCase;
        
        [Bindable]
        public LivesViewModel LivesVm { get; }
        
        [Bindable]
        public CoinViewModel CoinVM { get; }

        public HeartRefillPopupVM(
            LivesViewModel livesVm
            , CoinViewModel coinVm,
            RefillLivesWithCoinUseCase refillLivesWithCoinUseCase,
            AddLifeFromRewardUseCase addLifeFromRewardUseCase)
        {
            LivesVm = livesVm;
            CoinVM = coinVm;
            _refillLivesWithCoinUseCase = refillLivesWithCoinUseCase;
            _addLifeFromRewardUseCase = addLifeFromRewardUseCase;
            CloseCommand = new Command(Close);
            
            // 900코인으로 라이프 풀 채우기
            RefillLivesWithCoinCommand = new Command(
                () => RefillLivesWithCoinAsync().Forget(),
                () => 
                    CoinVM.Coin.Value >= RefillLivesWithCoinUseCase.DefaultCost
                    && !LivesVm.IsFull
            );

            // 리워드 광고 보고 라이프 +1
            WatchRewardAdForLifeCommand = new AsyncCommand(
                _ => WatchRewardAdForLifeAsync(),
                _ => ADS.IsBound && ADS.IsReadyRewarded && !LivesVm.IsFull
            );
        }

        async UniTaskVoid RefillLivesWithCoinAsync()
        {
            const int Cost = RefillLivesWithCoinUseCase.DefaultCost;

            var success = await _refillLivesWithCoinUseCase.ExecuteAsync(Cost);
            if (!success)
                return;

            CoinVM.RefreshAsync().Forget();
            LivesVm.RefreshOnceAsync().Forget();
        }

        // 리워드 광고 하트 +1 성공 시
        async UniTask WatchRewardAdForLifeAsync()
        {
            if (!ADS.IsBound || !ADS.IsReadyRewarded)
                return;

            var rewarded = await ADS.ShowRewardedAsync(AdReason.RewardBtn);
            if (!rewarded)
                return;

            var success = await _addLifeFromRewardUseCase.ExecuteAsync(1);
            if (!success)
                return;

            LivesVm.RefreshOnceAsync().Forget();
        }
    }
}