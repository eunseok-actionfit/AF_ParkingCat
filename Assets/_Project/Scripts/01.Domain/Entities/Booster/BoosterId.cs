using AES.Tools;


// DictKeyAttribute

namespace ParkingCat.Entities.Booster
{
    public enum BoosterId
    {
        [DictKey(3)] EscapeHint,       // 기본 3개
        [DictKey(2)] ForceExit,    // 기본 2개
    }
}
