namespace ParkingCat.Results
{
    public sealed class HandleLevelClearedResult
    {
        public long RewardCoin { get; }
        public int RewardLife { get; }

        public HandleLevelClearedResult(long rewardCoin, int rewardLife)
        {
            RewardCoin = rewardCoin;
            RewardLife = rewardLife;
        }
    }
}