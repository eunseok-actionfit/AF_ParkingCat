using ParkingCat.Move.Ports;
using ParkingCat.Input;

namespace ParkingCat.Move.UseCases
{
    public sealed class ForceExitUseCase
    {
        readonly IInputGate _gate;
        readonly IStateWriter _state;

        public ForceExitUseCase(IInputGate gate, IStateWriter state)
        {
            _gate = gate;
            _state = state;
        }

        public void Execute(int carId)
        {
            if (_gate.Locked) return;

            _gate.Lock();
            try
            {
                // 부스터는 이동수 소모 없음
                _state.ForceExitCar(carId, spendMove: false);
            }
            finally
            {
                _gate.Unlock();
            }
        }
    }
}