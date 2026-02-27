using AES.Tools;
using AES.Tools.UI.Popups;
using ParkingCat.Lobby.Popups;
using UnityEngine;


namespace ParkingCat.Game.Settings
{
    public sealed class InGameSettingsPopupVM : PopupViewModelBase
    {
        public Command CloseCommand { get; }
        public AsyncCommand RetryCommand { get; }
        public AsyncCommand QuitCommand { get; }
        
        public Command OpenPrivacyPolicyCommand { get; }
        public Command OpenTermsOfServiceCommand { get; }
        public AsyncCommand RestoreCommand { get; }

        [Bindable]
       public SettingViewModel m_settingViewModel;

        int m_stageId = 1; 

        public InGameSettingsPopupVM(
            SceneFlow.SceneFlow sceneFlow,
            SettingViewModel settingViewModel)
        {
            var sceneFlow1 = sceneFlow;
            m_settingViewModel = settingViewModel;
            CloseCommand = new Command(Close);
            RetryCommand = new AsyncCommand(_ => sceneFlow1.GoInGameAsync(m_stageId));
            QuitCommand = new AsyncCommand(_ => sceneFlow1.GoLobbyAsync());
            
            OpenPrivacyPolicyCommand = new Command(
                () => Application.OpenURL(ContactUtil.Privacy_Policy_EN));
            
            OpenTermsOfServiceCommand = new Command(
                ContactUtil.Contact);

            RestoreCommand =  new AsyncCommand(
                _ => IAP.RestoreAsync(),
                _ => IAP.IsReady);
        }
        
        public void SetStageId(int stageId) => this.m_stageId = stageId;
    }
}