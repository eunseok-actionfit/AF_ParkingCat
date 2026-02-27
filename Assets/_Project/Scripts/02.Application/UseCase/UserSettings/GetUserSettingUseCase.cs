using Cysharp.Threading.Tasks;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.UserSettings
{
    public class GetUserSettingUseCase
    {
        ISettingsRepository _repository;

        public GetUserSettingUseCase(ISettingsRepository repository)
        {
            _repository = repository;
        }

        public async UniTask<Entities.UserSettings> Execute()
        {
            return await _repository.LoadAsync();
        }
    }
}


