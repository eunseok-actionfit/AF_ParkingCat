using Cysharp.Threading.Tasks;


namespace ParkingCat.Repository
{
    public interface IPlayerStateRepository
    {
        UniTask<Entities.PlayerState> LoadAsync();
        UniTask SaveAsync(Entities.PlayerState data);
    }
}


