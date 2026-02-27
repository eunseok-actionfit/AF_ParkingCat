using ParkingCat.ValueObjects;


namespace ParkingCat.TurnProgram.DTOs
{
    public sealed class SetTurnProgramRequest
    {
        public int CarId;
        public TurnAction[] Actions = System.Array.Empty<TurnAction>();
        public bool Loop;
        public bool ResetCursor = true;
    }
}