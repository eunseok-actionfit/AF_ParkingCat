using System;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Economy.Lives
{
    public sealed class AddUnlimitedLivesMinutesUseCase
    {
        readonly IPlayerEconomyRepository _repo;
        readonly LivesConfig _config;

        public AddUnlimitedLivesMinutesUseCase(IPlayerEconomyRepository repo, LivesConfig config)
        {
            _repo = repo;
            _config = config;
        }

        public async UniTask ExecuteAsync(int minutes)
        {
            if (minutes <= 0) return;

            var eco = await _repo.LoadAsync();
            eco.AddUnlimitedLivesMinutes(minutes, DateTime.UtcNow);
            await _repo.SaveAsync(eco);
        }
    }
}