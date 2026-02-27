using System;
using System.Collections.Generic;
using AES.Tools;


namespace ParkingCat.Repository.Booster
{
    [Serializable]
    public class BoosterInventoryEntryData
    {
        public string id;   // enum 이름 or 코드
        public int count;
    }

    [Serializable]
    [SaveData("booster", UseSlot = true, Backend = SaveBackend.CloudFirst)]
    public class BoosterInventoryData
    {
        public List<BoosterInventoryEntryData> boosters = new();
    }
}