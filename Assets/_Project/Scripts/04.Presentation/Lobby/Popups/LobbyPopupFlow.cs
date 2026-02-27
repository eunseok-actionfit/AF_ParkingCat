using System;
using AES.Tools.UI.Popups;
using Cysharp.Threading.Tasks;
using ParkingCat.Lobby.Popups.AdsRemove;
using ParkingCat.Lobby.Popups.HeartRefill;
using ParkingCat.Lobby.Popups.Settings;


namespace ParkingCat.Lobby.Popups
{
    public sealed class LobbyPopupFlow : ILobbyPopupFlow
    {
        readonly IPopupService _popups;

        readonly Func<SettingsPopupVM> _settingsVm;
        readonly Func<HeartRefillPopupVM> _heartVm;
        readonly Func<AdsRemovePopupVM> _adsVm;

        public LobbyPopupFlow(
            IPopupService popups,
            Func<SettingsPopupVM> settingsVm,
            Func<HeartRefillPopupVM> heartVm,
            Func<AdsRemovePopupVM> adsVm)
        {
            _popups = popups;
            _settingsVm = settingsVm;
            _heartVm = heartVm;
            _adsVm = adsVm;
        }

        public async UniTask ShowSettingsAsync()
        {
            var vm = _settingsVm();
            if (_popups.TryShow<SettingsPopupView>(vm, out var task, modal: true))
                await task;
        }

        public async UniTask ShowHeartRefillAsync()
        {
            var vm = _heartVm();
            if (_popups.TryShow<HeartRefillPopupView>(vm, out var task, modal: true))
                await task;
        }

        public async UniTask ShowAdsRemoveAsync()
        {
            var vm = _adsVm();
            if (_popups.TryShow<AdsRemovePopupView>(vm, out var task, modal: true))
                await task;
        }
    }
}