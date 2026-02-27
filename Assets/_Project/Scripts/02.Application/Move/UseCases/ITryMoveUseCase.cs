using ParkingCat.Sim;


namespace ParkingCat.Move.UseCases
{
    public interface ITryMoveUseCase
    {
        SimResult Execute(int moverCarId);
    }
}