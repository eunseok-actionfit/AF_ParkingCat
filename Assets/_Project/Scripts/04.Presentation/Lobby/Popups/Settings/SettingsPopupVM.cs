using AES.Tools;
using AES.Tools.UI.Popups;
using UnityEngine;


namespace ParkingCat.Lobby.Popups.Settings
{
    public sealed class SettingsPopupVM : PopupViewModelBase
    {
        public Command CloseCommand { get; }
        
        public Command OpenPrivacyPolicyCommand { get; }
        public Command OpenTermsOfServiceCommand { get; }
        public AsyncCommand RestoreCommand { get; }

        [Bindable]
       public SettingViewModel m_settingViewModel;

        public SettingsPopupVM(SettingViewModel settingViewModel)
        {
            m_settingViewModel = settingViewModel;
            CloseCommand = new Command(Close);
            
            OpenPrivacyPolicyCommand = new Command(
                () => Application.OpenURL(ContactUtil.Privacy_Policy_EN));
            
            OpenTermsOfServiceCommand = new Command(
                ContactUtil.Contact);

            RestoreCommand =  new AsyncCommand(
                _ => IAP.RestoreAsync(),
                _ => IAP.IsReady);
        }
    }
}