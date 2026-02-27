using System;


namespace ParkingCat.Entities.Economy
{
    public sealed class PlayerEconomy
    {
        public long Coin { get; private set; }
        public int Gem  { get; private set; }

        /// <summary>현재 라이프</summary>
        public int Lives { get; private set; }

        /// <summary>라이프 회복 기준이 되는 마지막 UTC 시각</summary>
        public DateTime LivesLastUpdatedUtc { get; private set; }
        
        public DateTime UnlimitedLivesEndUtc { get; private set; } 
        
        
        public bool RemoveAds { get; private set; }  

        public PlayerEconomy(long coin, int gem, int lives, DateTime livesLastUpdatedUtc, DateTime unlimitedLivesEndUtc, bool removeAds)
        {
            Coin  = coin < 0 ? 0 : coin;
            Gem   = gem < 0 ? 0 : gem;
            Lives = lives < 0 ? 0 : lives;

            LivesLastUpdatedUtc =
                livesLastUpdatedUtc == default
                    ? DateTime.UtcNow
                    : livesLastUpdatedUtc;
            
            UnlimitedLivesEndUtc = unlimitedLivesEndUtc;
            RemoveAds = removeAds;
        }

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            Coin += amount;
        }

        public bool TrySpendCoin(int amount)
        {
            if (amount <= 0) return true;
            if (Coin < amount) return false;
            Coin -= amount;
            return true;
        }

        public void AddGem(int amount)
        {
            if (amount <= 0) return;
            Gem += amount;
        }

        public bool TrySpendGem(int amount)
        {
            if (amount <= 0) return true;
            if (Gem < amount) return false;
            Gem -= amount;
            return true;
        }
        
        public TimeSpan GetNextLifeRegenRemaining(DateTime nowUtc, int maxLives, TimeSpan regenInterval)
        {
            if (maxLives <= 0) return TimeSpan.Zero;
            
            if (Lives >= maxLives) return TimeSpan.Zero;

            if (nowUtc <= LivesLastUpdatedUtc) return regenInterval;

            var elapsed = nowUtc - LivesLastUpdatedUtc;
            var ticksIntoInterval = elapsed.Ticks % regenInterval.Ticks;
            var remainingTicks = regenInterval.Ticks - ticksIntoInterval;

            // 막 회복 타이밍에 걸린 경우 0이 나올 수 있으니 clamp
            if (remainingTicks <= 0) return TimeSpan.Zero;

            return TimeSpan.FromTicks(remainingTicks);
        }

        
        public bool HasUnlimitedLives(DateTime nowUtc)
            => UnlimitedLivesEndUtc > nowUtc;

        public void AddUnlimitedLivesMinutes(int minutes, DateTime nowUtc)
        {
            if (minutes <= 0) return;
            var baseTime = UnlimitedLivesEndUtc > nowUtc ? UnlimitedLivesEndUtc : nowUtc;
            UnlimitedLivesEndUtc = baseTime.AddMinutes(minutes);
        }
        
        /// <summary>
        /// 라이프를 강제로 풀로 채운다.
        /// </summary>
        public void SetLivesFull(DateTime nowUtc, int maxLives)
        {
            if (maxLives < 0) maxLives = 0;

            Lives = maxLives;
            LivesLastUpdatedUtc = nowUtc;
        }
        
        /// <summary>
        /// 외부 보상(IAP, 이벤트 등)으로 라이프를 추가.
        /// 상한을 적용하지 않는다.
        /// </summary>
        public void AddLifeReward(int amount, DateTime nowUtc)
        {
            if (amount <= 0) return;
            Lives += amount;
            LivesLastUpdatedUtc = nowUtc;
        }
        
        /// <summary>
        /// 자연 회복이 반영된 후, 외부 보상으로 라이프를 amount 만큼 추가.
        /// 이미 maxLives 이상이면 false (추가 실패).
        /// </summary>
        public bool TryAddLife(int amount, DateTime nowUtc, int maxLives)
        {
            if (amount <= 0) return true;

            if (Lives >= maxLives)
                return false;

            var newLives = Lives + amount;
            if (newLives > maxLives)
                newLives = maxLives;

            Lives = newLives;
            LivesLastUpdatedUtc = nowUtc;
            return true;
        }

        /// <summary>
        /// 현재 시간 기준으로 라이프를 재계산.
        /// </summary>
        public void TickLives(DateTime nowUtc, int maxLives, TimeSpan regenInterval)
        {
            if (maxLives <= 0)
            {
                Lives = 0;
                LivesLastUpdatedUtc = nowUtc;
                return;
            }

            // 이미 maxLives 이상이면 자연 회복은 적용하지 않는다.
            // (보상으로 maxLives를 초과한 상태일 수 있으므로 Lives를 clamp 하지 않는다.)
            if (Lives >= maxLives)
            {
                // 여기서 LivesLastUpdatedUtc를 nowUtc로 갱신하지 않는 것을 권장:
                // - 초과 상태에서는 회복 타이머를 굳이 움직일 필요가 없음
                // - 이후 소비(TryConsumeLife)에서 LivesLastUpdatedUtc를 nowUtc로 세팅하므로
                //   그 시점부터 회복 타이머가 정상 시작됨
                return;
            }

            if (nowUtc <= LivesLastUpdatedUtc)
                return;

            var elapsed = nowUtc - LivesLastUpdatedUtc;
            var regenCount = (int)(elapsed.Ticks / regenInterval.Ticks);

            if (regenCount <= 0)
                return;

            var newLives = Math.Min(maxLives, Lives + regenCount);
            var actuallyAdded = newLives - Lives;

            Lives = newLives;
            // 실제로 채워진 만큼만 기준 시각을 앞으로 이동
            LivesLastUpdatedUtc += TimeSpan.FromTicks(regenInterval.Ticks * actuallyAdded);
        }

        /// <summary>
        /// 현재 시간 기준으로 라이프를 갱신한 뒤, 1 소모 시도
        /// </summary>
        public bool TryConsumeLife(DateTime nowUtc, int maxLives, TimeSpan regenInterval, int amount = 1)
        {
            if (amount <= 0) return true;

            TickLives(nowUtc, maxLives, regenInterval);

            if (Lives < amount)
                return false;

            Lives -= amount;
            LivesLastUpdatedUtc = nowUtc;
            return true;
        }
        

        public void SetRemoveAds(bool value) => RemoveAds = value;
    }
}
