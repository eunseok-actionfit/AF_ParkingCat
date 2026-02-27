using AES.Tools;
using AES.Tools.UI.Popups;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat.Economy;
using ParkingCat.Entities.Booster;
using ParkingCat.Game.Booster;
using ParkingCat.UseCase.Booster;


namespace ParkingCat.Game.BuyHint
{
    public sealed class BuyUFOPopupVM : PopupViewModelBase
    {
        [Bindable]
        public readonly int DefaultCost = 1800;
        [Bindable]
        public readonly int DefaultAmount = 3;

        public Command CloseCommand { get; }

        public Command Buy3WithCoinCommand { get; }
        public AsyncCommand WatchRewardAdForHintCommand { get; }

        readonly CoinViewModel _coinVm;
        readonly BoosterViewModel _hintVm;

        public BuyUFOPopupVM(
            CoinViewModel coinVm,
            BoosterBarViewModel boosterBarViewModel)
        {
            _coinVm = coinVm;
            _hintVm = boosterBarViewModel.Get(BoosterId.ForceExit);

            CloseCommand = new Command(Close);

            Buy3WithCoinCommand = new Command(
                () => Buy3HintWithCoinAsync().Forget(),
                () => _coinVm.Coin.Value >= DefaultCost
            );

            WatchRewardAdForHintCommand = new AsyncCommand(
                _ => WatchRewardAdForHintAsync(),
                _ => ADS.IsBound && ADS.IsReadyRewarded
            );
        }

        async UniTaskVoid Buy3HintWithCoinAsync()
        {
            var success = await _coinVm.TrySpendAsync(DefaultCost);
            if (!success) return;

            await _hintVm.AddAsync(DefaultAmount);
            Close(); // 게임 재개
        }

        async UniTask WatchRewardAdForHintAsync()
        {
            var rewarded = await ADS.ShowRewardedAsync(AdReason.RewardBtn);
            if (!rewarded) return;

            await _hintVm.AddAsync(1);

            Close();
        }
    }
}