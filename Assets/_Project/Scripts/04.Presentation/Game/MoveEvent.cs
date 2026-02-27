using ParkingCat.Sim;


namespace ParkingCat.Game
{
    public sealed class MoveEvent
    {
        public int Seq;
        public int CarId;
        public SimResult Result;

        public MoveEvent(int seq, int carId, SimResult result)
        {
            Seq = seq;
            CarId = carId;
            Result = result;
        }
    }
}