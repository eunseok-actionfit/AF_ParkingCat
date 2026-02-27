using Cysharp.Threading.Tasks;


namespace ParkingCat.Repository
{
    public interface IPlayerProgressRepository
    {
        Entities.PlayerProgress Current { get; } 
        
        UniTask<Entities.PlayerProgress> LoadAsync();
        UniTask SaveAsync(Entities.PlayerProgress data);
    }
}


