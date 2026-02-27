using Cysharp.Threading.Tasks;


namespace ParkingCat.Repository
{
    public interface IPlayerEconomyRepository
    {
        Entities.Economy.PlayerEconomy Current { get; } 
        UniTask<Entities.Economy.PlayerEconomy> LoadAsync();
        UniTask SaveAsync(Entities.Economy.PlayerEconomy data);
    }
}


