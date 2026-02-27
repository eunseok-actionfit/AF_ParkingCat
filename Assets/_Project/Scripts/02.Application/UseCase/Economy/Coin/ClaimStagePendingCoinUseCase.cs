using Cysharp.Threading.Tasks;
using ParkingCat.Repository;
using ParkingCat.UseCase.Economy.Coin;

public sealed class ClaimStagePendingCoinUseCase
{
    readonly IStageRunRepository _stageRunRepo;
    readonly AddCoinUseCase _addCoin;

    public ClaimStagePendingCoinUseCase(
        IStageRunRepository stageRunRepo,
        AddCoinUseCase addCoin)
    {
        _stageRunRepo = stageRunRepo;
        _addCoin = addCoin;
    }

    public async UniTask<int> ExecuteAsync()
    {
        var run = _stageRunRepo.Get();
        int amount = run.ClaimPendingCoin();
        _stageRunRepo.Save(run);

        if (amount > 0)
            await _addCoin.ExecuteAsync(amount);

        return amount;
    }
}