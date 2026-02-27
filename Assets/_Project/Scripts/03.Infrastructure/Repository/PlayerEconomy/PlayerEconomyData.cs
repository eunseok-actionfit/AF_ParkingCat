using System;
using AES.Tools;


namespace ParkingCat.Repository.PlayerEconomy
{
    [Serializable]
    [SaveData("economy", UseSlot = true, Backend = SaveBackend.CloudFirst)]
    public class PlayerEconomyData
    {
        public long coin;
        public int gem;
        public int lives;
        
        // DateTime.UtcNow.Ticks 저장용
        public DateTime livesLastUpdatedUtc;
        // 무제한 만료 시간
        public DateTime unlimitedLivesEndUtc;
        
        public bool removeAds;  
    }
}


