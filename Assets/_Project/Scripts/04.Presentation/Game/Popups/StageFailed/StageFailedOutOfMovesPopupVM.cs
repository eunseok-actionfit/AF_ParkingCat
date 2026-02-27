using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat;
using ParkingCat.Economy;
using ParkingCat.SceneFlow;
using ParkingCat.UseCase;

public sealed class StageFailedOutOfMovesPopupVM : StageFailedPopupVM
{
    readonly AddMovesWithCoinUseCase _coinUseCase;
    readonly AddMovesFromRewardUseCase _rewardUseCase;

    public StageFailedOutOfMovesPopupVM(
        CoinViewModel coinVm,
        AddMovesWithCoinUseCase addMovesWithCoin,
        AddMovesFromRewardUseCase addMovesFromReward,
        SceneFlow flow)
        : base(coinVm, flow)
    {
        _coinUseCase = addMovesWithCoin;
        _rewardUseCase = addMovesFromReward;
    }

    public override string Title => "out_of_moves.title";
    public override string Desc => "out_of_moves.desc";
    public override string PrimaryLabel => "out_of_moves.primary";
    public override string SecondaryLabel => "out_of_moves.secondary";

    protected override bool CanContinueWithCoin()
        => CoinVM.Coin.Value >= AddMovesWithCoinUseCase.DefaultCost;

    protected override async UniTask<bool> TryContinueWithCoinAsync()
    {
        var success = await _coinUseCase.ExecuteAsync();
        if (!success) return false;

        CoinVM.RefreshAsync().Forget();
        return true; // 베이스가 Close() 처리
    }

    protected override async UniTask<bool> TryContinueWithRewardAsync()
    {
        var rewarded = await ADS.ShowRewardedAsync(AdReason.RewardBtn);
        if (!rewarded) return false;

        return await _rewardUseCase.ExecuteAsync();
    }
}