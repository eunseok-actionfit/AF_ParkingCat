using Cysharp.Threading.Tasks;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.UserSettings
{
    public class SetUserSettingUseCase
    {
        ISettingsRepository _repository;

        public SetUserSettingUseCase(ISettingsRepository repository)
        {
            _repository = repository;
        }
        
        public async UniTask Execute(Entities.UserSettings settings)
        {
            await _repository.SaveAsync(settings);
        }
    }
}


