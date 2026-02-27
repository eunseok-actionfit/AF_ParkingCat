using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Booster
{
    public sealed class ConsumeBoosterUseCase
    {
        readonly IBoosterInventoryRepository _repo;

        public ConsumeBoosterUseCase(IBoosterInventoryRepository repo)
        {
            _repo = repo;
        }

        public async UniTask<bool> ExecuteAsync(BoosterId id, int amount = 1)
        {
            var inv = await _repo.LoadAsync();
            var ok = inv.TryConsume(id, amount);
            if (!ok)
                return false;

            await _repo.SaveAsync(inv);
            return true;
        }
    }
}