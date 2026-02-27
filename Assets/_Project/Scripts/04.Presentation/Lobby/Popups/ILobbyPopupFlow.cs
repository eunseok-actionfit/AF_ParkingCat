using Cysharp.Threading.Tasks;


namespace ParkingCat.Lobby.Popups
{
    public interface ILobbyPopupFlow
    {
        UniTask ShowSettingsAsync();
        UniTask ShowHeartRefillAsync();
        UniTask ShowAdsRemoveAsync();
    }
}