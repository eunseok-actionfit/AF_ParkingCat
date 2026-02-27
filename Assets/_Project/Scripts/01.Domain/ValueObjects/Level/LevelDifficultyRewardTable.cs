namespace ParkingCat.ValueObjects.Level
{
    public sealed class LevelDifficultyRewardTable : ILevelDifficultyRewardTable
    {
        public int GetCoinReward(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Normal:
                    return 20;

                case LevelDifficulty.Hard:
                    return 60;

                case LevelDifficulty.VeryHard:
                    return 100;
                default:
                    return 20; 
            }
        }
    }
}