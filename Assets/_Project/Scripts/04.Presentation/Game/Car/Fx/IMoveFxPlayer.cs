using System;
using Cysharp.Threading.Tasks;
using ParkingCat.Sim;


namespace ParkingCat.Game.Car.Fx
{
    public interface IMoveFxPlayer
    {
        bool IsPlaying { get; }
        event Action OnAllFxCompleted;
        UniTask PlayAsync(int moverCarId, SimResult result);
    }
}