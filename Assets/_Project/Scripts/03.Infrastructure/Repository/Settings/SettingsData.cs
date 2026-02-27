using System;
using AES.Tools;


namespace ParkingCat.Repository.Settings
{
    [Serializable]
    [SaveData("settings", UseSlot = false)]
    public sealed class SettingsData
    {
        public float bgmVolume = 0.8f;
        public bool isBgmMuted = false;
        public float sfxVolume = 0.8f;
        public bool isSfxMuted = false;
        public bool vibration = true;
        public string language = "ko-KR";
    }
    

}


