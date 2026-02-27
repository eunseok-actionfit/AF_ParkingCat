namespace ParkingCat
{
    public enum RunStatus
    {
        Running,
        Win,
        Lose
    }

    public readonly struct StageRules
    {
        public readonly bool HasMoveLimit;
        public readonly int MoveLimit;

        public StageRules(bool hasMoveLimit, int moveLimit)
        {
            HasMoveLimit = hasMoveLimit;
            MoveLimit = moveLimit;
        }

        public static StageRules NoLimit => new StageRules(false, 0);
    }
}