using Cysharp.Threading.Tasks;
using ParkingCat.Move.Ports;
using ParkingCat.State;


public sealed class AddAmbulanceLimitFromRewardUseCase
{
    readonly IStateWriter _writer;

    public AddAmbulanceLimitFromRewardUseCase(IStateWriter writer)
    {
        _writer = writer;
    }

    public UniTask<bool> ExecuteAsync(int add = 5)
    {
        _writer.AddAmbulanceEscapeMoves(add);
        return UniTask.FromResult(true);
    }
}