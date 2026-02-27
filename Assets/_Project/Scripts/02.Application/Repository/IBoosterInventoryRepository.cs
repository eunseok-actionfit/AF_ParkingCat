using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;


namespace ParkingCat.Repository
{
    public interface IBoosterInventoryRepository
    {
        UniTask<BoosterInventory> LoadAsync();
        UniTask SaveAsync(BoosterInventory inv);
    }
}