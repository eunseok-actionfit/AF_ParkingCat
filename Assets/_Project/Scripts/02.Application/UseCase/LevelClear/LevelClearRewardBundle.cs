using System;
using ParkingCat.Entities.Booster;


namespace ParkingCat.UseCase.LevelClear
{
    public readonly struct LevelClearRewardBundle
    {
        public readonly int Coin;
        public readonly int Lives; // 보상용(상한 무시)
        public readonly (BoosterId id, int amount)[] Boosters;

        public LevelClearRewardBundle(int coin, int lives, (BoosterId, int)[] boosters = null)
        {
            Coin = coin;
            Lives = lives;
            Boosters = boosters ?? Array.Empty<(BoosterId, int)>();
        }

        public bool IsEmpty => Coin <= 0 && Lives <= 0 && Boosters.Length == 0;
    }
}