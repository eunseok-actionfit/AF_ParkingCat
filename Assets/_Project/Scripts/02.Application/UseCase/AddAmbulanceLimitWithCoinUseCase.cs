using Cysharp.Threading.Tasks;
using ParkingCat.Move.Ports;
using ParkingCat.State;
using ParkingCat.UseCase.Economy.Coin;


public sealed class AddAmbulanceLimitWithCoinUseCase
{
    readonly IStateWriter _writer;
    readonly SpendCoinUseCase _spendCoin;

    public const int DefaultCost = 900;

    public AddAmbulanceLimitWithCoinUseCase(IStateWriter writer, SpendCoinUseCase spendCoin)
    {
        _writer = writer;
        _spendCoin = spendCoin;
    }

    public async UniTask<bool> ExecuteAsync(int add = 5, int cost = DefaultCost)
    {
        var paid = await _spendCoin.ExecuteAsync(cost);
        if (!paid) return false;

        _writer.AddAmbulanceEscapeMoves(add);
        return true;
    }
}