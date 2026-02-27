using AES.Tools;
using AES.Tools.UI.Popups;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat;
using ParkingCat.Economy;
using ParkingCat.SceneFlow;


public abstract class StageFailedPopupVM : PopupViewModelBase
{
    // 로비로(실패 포기)
    public AsyncCommand BackToLobbyCommand { get; }

    // 계속하기
    public Command ContinueWithCoinCommand { get; }
    public AsyncCommand ContinueWithRewardCommand { get; }

    [Bindable] public abstract string Title { get; }
    [Bindable] public abstract string Desc { get; }
    [Bindable] public abstract string PrimaryLabel { get; }
    [Bindable] public abstract string SecondaryLabel { get; }

    protected readonly CoinViewModel CoinVM;

    protected StageFailedPopupVM(CoinViewModel coinVm, SceneFlow flow)
    {
        CoinVM = coinVm;

        BackToLobbyCommand = new AsyncCommand(async _ =>
        {
            await ADS.ShowInterstitialAsync(AdReason.StageFail);
            flow.GoLobbyAsync().Forget();
        });

        ContinueWithCoinCommand = new Command(
            () => ContinueWithCoinImpl().Forget(),
            CanContinueWithCoin
        );

        ContinueWithRewardCommand = new AsyncCommand(
            _ => ContinueWithRewardImpl(),
            _ => ADS.IsBound && ADS.IsReadyRewarded
        );
    }

    protected abstract bool CanContinueWithCoin();

    // 파생 VM은 "보상 적용 성공/실패"만 반환
    protected abstract UniTask<bool> TryContinueWithCoinAsync();
    protected abstract UniTask<bool> TryContinueWithRewardAsync();

    async UniTaskVoid ContinueWithCoinImpl()
    {
        var ok = await TryContinueWithCoinAsync();
        if (!ok) return;

        // 팝업만 닫고 게임 재개(상태/다음 팝업은 Flow가 책임)
        Close();
    }

    async UniTask ContinueWithRewardImpl()
    {
        var ok = await TryContinueWithRewardAsync();
        if (!ok) return;

        Close();
    }
}