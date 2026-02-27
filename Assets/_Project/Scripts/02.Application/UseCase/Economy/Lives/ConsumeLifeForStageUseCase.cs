using System;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;


namespace ParkingCat.UseCase.Economy.Lives
{
    public sealed class ConsumeLifeForStageUseCase
    {
        readonly IPlayerEconomyRepository _economyRepository;
        readonly ITimeProvider _timeProvider;
        readonly LivesConfig _config;

        public ConsumeLifeForStageUseCase(
            IPlayerEconomyRepository economyRepository,
            ITimeProvider timeProvider,
            LivesConfig config)
        {
            _economyRepository = economyRepository;
            _timeProvider      = timeProvider;
            _config            = config;
        }

        /// <summary>
        /// 라이프 1개 소비 시도. 실패 시 false.
        /// </summary>
        public async UniTask<bool> ExecuteAsync()
        {
            var economy = await _economyRepository.LoadAsync();
            
            if (economy.HasUnlimitedLives(DateTime.UtcNow))
                return true;

            var success = economy.TryConsumeLife(
                _timeProvider.UtcNow,
                _config.MaxLives,
                _config.RegenInterval,
                1
            );

            if (!success)
                return false;

            await _economyRepository.SaveAsync(economy);
            return true;
        }
    }
}