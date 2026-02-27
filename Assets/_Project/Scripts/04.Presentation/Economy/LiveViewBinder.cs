using System;
using AES.Tools;
using AES.Tools.TBC.Collections;
using ParkingCat.Lobby;
using TMPro;
using UnityEngine;


namespace ParkingCat.Economy
{

    public class LiveViewBinder : ContextListenerBase
    {
        [SerializeField] TMP_Text m_amountText;
        [SerializeField] TMP_Text m_RemainText;
        [SerializeField] TMP_Text m_InfinityText;

        private LivesViewModel _vm;
        private CompositeDisposable _disposable;

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            var vm = ctx.GetValue() as LobbySceneViewModel;
            if (vm == null) return;
            _vm = vm.LivesVm;

            _disposable = new CompositeDisposable();

            _vm.CurrentLives
                .Subscribe(amount =>
                {
                    string text = _vm.IsInfinityMode.Value ? "∞" :  amount.ToString();
                    m_amountText.text = text;
                })
                .AddTo(_disposable);

            _vm.IsInfinityMode
                .Subscribe(isInfinity =>
                {
                    m_amountText.text = isInfinity ? "∞" : _vm.CurrentLives.Value.ToString();
                    m_InfinityText.enabled = isInfinity;
                    m_RemainText.enabled = !isInfinity;
                })
                .AddTo(_disposable);
        }

        protected override void OnContextUnavailable()
        {
            _disposable?.Dispose();
            _disposable = null;
            _vm = null;
        }
    }

}