namespace ParkingCat.Service
{
    public interface IMenuService
    {
        void OpenSettings();
        bool IsSettingsOpen { get; }

        void OpenHeartRefill();
        bool IsHeartRefillOpen { get; }

        void OpenAdsRemove();
        bool IsAdsRemoveOpen { get; }
    }
}

