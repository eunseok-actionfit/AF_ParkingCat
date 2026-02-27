using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;


namespace ParkingCat.UseCase.Economy.Lives
{
    public sealed class RefreshLivesUseCase
    {
        readonly IPlayerEconomyRepository _economyRepository;
        readonly ITimeProvider _timeProvider;
        readonly LivesConfig _config;

        public RefreshLivesUseCase(
            IPlayerEconomyRepository economyRepository,
            ITimeProvider timeProvider,
            LivesConfig config)
        {
            _economyRepository = economyRepository;
            _timeProvider      = timeProvider;
            _config            = config;
        }

        public async UniTask<PlayerEconomy> ExecuteAsync()
        {
            var economy = await _economyRepository.LoadAsync();

            economy.TickLives(
                _timeProvider.UtcNow,
                _config.MaxLives,
                _config.RegenInterval
            );

            await _economyRepository.SaveAsync(economy);
            return economy;
        }
    }
}