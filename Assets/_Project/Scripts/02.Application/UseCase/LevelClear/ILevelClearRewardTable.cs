using ParkingCat.ValueObjects.Level;


namespace ParkingCat.UseCase.LevelClear
{
    public interface ILevelClearRewardTable
    {
        LevelClearRewardBundle GetReward(LevelDifficulty difficulty);
    }
}