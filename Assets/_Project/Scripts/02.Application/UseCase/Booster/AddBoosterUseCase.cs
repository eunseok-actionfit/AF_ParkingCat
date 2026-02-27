using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Booster
{
    public sealed class AddBoosterUseCase
    {
        readonly IBoosterInventoryRepository _repo;

        public AddBoosterUseCase(IBoosterInventoryRepository repo)
        {
            _repo = repo;
        }

        public async UniTask ExecuteAsync(BoosterId id, int amount)
        {
            if (amount <= 0) return;

            var inv = await _repo.LoadAsync();
            inv.Add(id, amount);
            await _repo.SaveAsync(inv);
        }
    }
}