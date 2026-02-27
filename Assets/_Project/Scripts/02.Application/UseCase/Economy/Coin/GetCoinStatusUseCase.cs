using Cysharp.Threading.Tasks;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Economy.Coin
{
    public sealed  class GetCoinStatusUseCase
    {
        readonly IPlayerEconomyRepository _repo;

        public GetCoinStatusUseCase(IPlayerEconomyRepository repo)
        {
            _repo = repo;
        }

        public async UniTask<long> ExecuteAsync()
        {
            var economy = await _repo.LoadAsync();
            return economy.Coin;
        }
    }
}