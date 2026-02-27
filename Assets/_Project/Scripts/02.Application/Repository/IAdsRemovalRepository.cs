using Cysharp.Threading.Tasks;


namespace ParkingCat.Repository
{
    public interface IAdsRemovalRepository
    {
        UniTask<bool> GetRemovedAsync();
        UniTask SetRemovedAsync(bool removed);
    }
}