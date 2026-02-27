using Cysharp.Threading.Tasks;
using ParkingCat.Results;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.UseCase.LevelClear
{
    public sealed class HandleLevelClearedUseCase
    {
        readonly SaveLevelClearProgressUseCase _saveClearProgress;
        readonly GrantLevelClearRewardsUseCase _grantRewards;
        readonly ClaimStagePendingCoinUseCase _claimPendingCoin;

        public HandleLevelClearedUseCase(
            SaveLevelClearProgressUseCase saveClearProgress,
            GrantLevelClearRewardsUseCase grantRewards,
            ClaimStagePendingCoinUseCase claimPendingCoin)
        {
            _saveClearProgress = saveClearProgress;
            _grantRewards = grantRewards;
            _claimPendingCoin = claimPendingCoin;
        }

        public async UniTask<HandleLevelClearedResult> ExecuteAsync(int levelId, LevelDifficulty difficulty)
        {
            await _saveClearProgress.ExecuteAsync(levelId);

            // 1) pendingCoin 지급
            int claimedCoin = await _claimPendingCoin.ExecuteAsync();

            // 2) 난이도 보상 (Lives / Booster만)
            var reward = await _grantRewards.ExecuteAsync(difficulty);

            return new HandleLevelClearedResult(claimedCoin, reward.Lives);
        }

    }
}