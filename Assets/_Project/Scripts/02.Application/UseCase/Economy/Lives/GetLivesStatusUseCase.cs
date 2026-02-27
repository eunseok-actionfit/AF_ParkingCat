using System;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;


namespace ParkingCat.UseCase.Economy.Lives
{
    public sealed class GetLivesStatusUseCase
    {
        readonly IPlayerEconomyRepository _repo;
        readonly ITimeProvider _time;
        readonly LivesConfig _config;

        public GetLivesStatusUseCase(
            IPlayerEconomyRepository repo,
            ITimeProvider time,
            LivesConfig config)
        {
            _repo = repo;
            _time = time;
            _config = config;
        }

        public async UniTask<LivesStatus> ExecuteAsync()
        {
            var now = DateTime.UtcNow;
            var economy = await _repo.LoadAsync();

            bool isUnlimited = economy.HasUnlimitedLives(now);
            TimeSpan unlimitedRemaining = isUnlimited
                ? economy.UnlimitedLivesEndUtc - now
                : TimeSpan.Zero;

            TimeSpan nextRegen = isUnlimited
                ? TimeSpan.Zero
                : economy.GetNextLifeRegenRemaining(now, _config.MaxLives, _config.RegenInterval);

            return new LivesStatus(
                economy.Lives,
                _config.MaxLives,
                nextRegen,
                isUnlimited,
                unlimitedRemaining
            );
        }
    }
}