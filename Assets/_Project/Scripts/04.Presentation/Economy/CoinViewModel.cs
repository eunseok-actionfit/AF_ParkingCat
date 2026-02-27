using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.UseCase.Economy.Coin;


namespace ParkingCat.Economy
{
    public sealed class CoinViewModel
    {
        public ObservableProperty<long> Coin { get; } = new();

        readonly GetCoinStatusUseCase _getCoinStatusUseCase;
        readonly SpendCoinUseCase _spendCoinUse;

        public CoinViewModel(GetCoinStatusUseCase getCoinStatusUseCase, SpendCoinUseCase spendCoinUse)
        {
            _getCoinStatusUseCase = getCoinStatusUseCase;
            _spendCoinUse = spendCoinUse;
            RefreshAsync().Forget();
        }


        public async UniTaskVoid RefreshAsync()
        {
            Coin.Value = await _getCoinStatusUseCase.ExecuteAsync();
        }

        public async UniTask<bool> TrySpendAsync(int amount)
        {
            var ok = await _spendCoinUse.ExecuteAsync(amount);
            RefreshAsync().Forget();
            return ok;
        }
    }
}