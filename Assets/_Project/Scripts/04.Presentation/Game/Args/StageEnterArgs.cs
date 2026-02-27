using System;


namespace ParkingCat.Game.Args
{
    [Serializable]
    public sealed class StageEnterArgs : ISceneArgs
    {
        public int StageNumber;
    }
}