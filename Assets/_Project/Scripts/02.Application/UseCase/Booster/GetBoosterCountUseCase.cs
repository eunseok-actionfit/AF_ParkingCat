using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Booster
{
    public sealed class GetBoosterCountUseCase
    {
        readonly IBoosterInventoryRepository _repo;

        public GetBoosterCountUseCase(IBoosterInventoryRepository repo)
        {
            _repo = repo;
        }

        public async UniTask<int> ExecuteAsync(BoosterId id)
        {
            var inv = await _repo.LoadAsync();
            return inv.GetCount(id);
        }
    }
}