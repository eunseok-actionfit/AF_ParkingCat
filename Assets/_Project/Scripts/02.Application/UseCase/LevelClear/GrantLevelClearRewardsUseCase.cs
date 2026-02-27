using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.UseCase.LevelClear
{
    public sealed class GrantLevelClearRewardsUseCase
    {
        readonly IPlayerEconomyRepository _economyRepo;
        readonly IBoosterInventoryRepository _boosterRepo;
        readonly ILevelClearRewardTable _rewardTable;
        readonly ITimeProvider _time;
        readonly LivesConfig _lives;

        public GrantLevelClearRewardsUseCase(
            IPlayerEconomyRepository economyRepo,
            IBoosterInventoryRepository boosterRepo,
            ILevelClearRewardTable rewardTable,
            ITimeProvider time,
            LivesConfig lives)
        {
            _economyRepo = economyRepo;
            _boosterRepo = boosterRepo;
            _rewardTable = rewardTable;
            _time = time;
            _lives = lives;
        }

        public async UniTask<LevelClearRewardBundle> ExecuteAsync(LevelDifficulty difficulty)
        {
            var reward = _rewardTable.GetReward(difficulty);
            if (reward.IsEmpty) return reward;

            // Economy: 1회 로드/세이브
            var economy = await _economyRepo.LoadAsync();                   
            economy.TickLives(_time.UtcNow, _lives.MaxLives, _lives.RegenInterval); 

          //  if (reward.Coin > 0) economy.AddCoin(reward.Coin);                 
            if (reward.Lives > 0) economy.TryAddLife(reward.Lives, _time.UtcNow, _lives.MaxLives);

            await _economyRepo.SaveAsync(economy);                              

            // Booster: 보상이 있을 때만 1회 로드/세이브
            if (reward.Boosters.Length > 0)
            {
                var inv = await _boosterRepo.LoadAsync();        
                foreach (var (id, amount) in reward.Boosters)
                    inv.Add(id, amount);                                      
                await _boosterRepo.SaveAsync(inv);                            
            }

            return reward;
        }
    }
}
