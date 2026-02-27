using System;
using AES.Tools;


namespace ParkingCat.Repository.PlayerProgress
{
    [Serializable]
    [SaveData( "player_progress", UseSlot = false, Backend = SaveBackend.CloudFirst)]
    public sealed class PlayerProgressData
    {
        public int lastClearedLevel;
    }
}


