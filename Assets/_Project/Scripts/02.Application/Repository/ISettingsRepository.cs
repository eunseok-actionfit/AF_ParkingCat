using Cysharp.Threading.Tasks;
using ParkingCat.Entities;


namespace ParkingCat.Repository
{
    public interface ISettingsRepository
    {
        UniTask<UserSettings> LoadAsync();
        UniTask SaveAsync(UserSettings data);
    }
}


