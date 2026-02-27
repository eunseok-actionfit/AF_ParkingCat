namespace ParkingCat.ValueObjects.Level
{
    public interface ILevelDifficultyRewardTable
    {
        int GetCoinReward(LevelDifficulty difficulty);
    }
}


