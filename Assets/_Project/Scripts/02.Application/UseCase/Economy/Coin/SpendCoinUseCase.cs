using Cysharp.Threading.Tasks;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Economy.Coin
{
    public sealed class SpendCoinUseCase
    {
        readonly IPlayerEconomyRepository _repo;

        public SpendCoinUseCase(IPlayerEconomyRepository repo)
        {
            _repo = repo;
        }

        public async UniTask<bool> ExecuteAsync(int amount)
        {
            var economy = await _repo.LoadAsync();
            var ok = economy.TrySpendCoin(amount);

            if (!ok)
                return false;

            await _repo.SaveAsync(economy);
            return true;
        }
    }
}