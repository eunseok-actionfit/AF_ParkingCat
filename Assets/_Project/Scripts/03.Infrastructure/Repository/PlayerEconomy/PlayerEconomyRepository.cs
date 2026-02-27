using System;
using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;


namespace ParkingCat.Repository.PlayerEconomy
{
    public sealed class PlayerEconomyRepository : IPlayerEconomyRepository
    {
        readonly IStorageService _storage;
        readonly ISlotService _slot;
        readonly LivesConfig _config;   

        public PlayerEconomyRepository(
            IStorageService storage,
            ISlotService slot,
            LivesConfig config)             
        {
            _storage = storage;
            _slot    = slot;
            _config  = config;
        }

        public Entities.Economy.PlayerEconomy Current  { get; private set; } 

        public async UniTask<Entities.Economy.PlayerEconomy> LoadAsync()
        {
            var slotId = _slot.CurrentSlotId;
            var dto = await _storage.LoadAsync<PlayerEconomyData>(slotId);
            
            if (dto == null)
            {
                dto = new PlayerEconomyData
                {
                    coin  = 3000,
                    gem   = 0,
                    lives = _config.InitialLives,
                    livesLastUpdatedUtc = DateTime.UtcNow,
                    unlimitedLivesEndUtc = DateTime.MinValue,
                    removeAds = false
                };

                await _storage.SaveAsync(slotId, dto);
            }

            // DateTime 기본값/Kind 방어
            var lastUpdated = dto.livesLastUpdatedUtc;

            if (lastUpdated == default)
            {
                // 저장이 안 돼 있던 경우 현재 시간으로 초기화
                lastUpdated = DateTime.UtcNow;
            }
            else if (lastUpdated.Kind == DateTimeKind.Unspecified)
            {
                // 직렬화 과정에서 Kind 정보가 사라졌다면 UTC로 간주
                lastUpdated = DateTime.SpecifyKind(lastUpdated, DateTimeKind.Utc);
            }
            
            var endUtc = dto.unlimitedLivesEndUtc;
            if (endUtc.Kind == DateTimeKind.Unspecified)
                endUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
            
            Current = new Entities.Economy.PlayerEconomy(
                dto.coin,
                dto.gem,
                dto.lives,
                lastUpdated,
                endUtc,
                dto.removeAds
            );

            return Current;
        }

        public async UniTask SaveAsync(Entities.Economy.PlayerEconomy economy)
        {
            // 항상 UTC로 저장
            var utc = economy.LivesLastUpdatedUtc;
            if (utc.Kind != DateTimeKind.Utc)
                utc = utc.ToUniversalTime();
            
            var endUtc = economy.UnlimitedLivesEndUtc;
            if (endUtc.Kind != DateTimeKind.Utc)
                endUtc = endUtc.ToUniversalTime();


            var dto = new PlayerEconomyData
            {
                coin  = economy.Coin,
                gem   = economy.Gem,
                lives = economy.Lives,
                livesLastUpdatedUtc = utc,
                unlimitedLivesEndUtc = endUtc,
                removeAds = economy.RemoveAds
            };

            var slotId = _slot.CurrentSlotId;
            await _storage.SaveAsync(slotId, dto);
            
            Current = economy;
        }
    }
}
