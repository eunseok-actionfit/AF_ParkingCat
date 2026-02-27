using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat;
using ParkingCat.Economy;
using ParkingCat.SceneFlow;

public sealed class StageFailedAmbulancePopupVM : StageFailedPopupVM
{
    readonly AddAmbulanceLimitWithCoinUseCase _coinUseCase;
    readonly AddAmbulanceLimitFromRewardUseCase _rewardUseCase;

    public StageFailedAmbulancePopupVM(
        CoinViewModel coinVm,
        AddAmbulanceLimitWithCoinUseCase addWithCoin,
        AddAmbulanceLimitFromRewardUseCase addFromReward,
        SceneFlow flow)
        : base(coinVm, flow)
    {
        _coinUseCase = addWithCoin;
        _rewardUseCase = addFromReward;
    }

    public override string Title => "ambulance.title";
    public override string Desc => "ambulance.desc";
    public override string PrimaryLabel => "ambulance.primary";
    public override string SecondaryLabel => "ambulance.secondary";

    protected override bool CanContinueWithCoin()
        => CoinVM.Coin.Value >= AddAmbulanceLimitWithCoinUseCase.DefaultCost;

    protected override async UniTask<bool> TryContinueWithCoinAsync()
    {
        var success = await _coinUseCase.ExecuteAsync(add: 5);
        if (!success) return false;

        CoinVM.RefreshAsync().Forget();
        return true; // 베이스가 Close() 처리
    }

    protected override async UniTask<bool> TryContinueWithRewardAsync()
    {
        var rewarded = await ADS.ShowRewardedAsync(AdReason.RewardBtn);
        if (!rewarded) return false;

        return await _rewardUseCase.ExecuteAsync(add: 5);
    }
}