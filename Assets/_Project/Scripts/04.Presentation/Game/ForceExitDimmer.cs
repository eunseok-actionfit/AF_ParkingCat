using System;
using AES.Tools;
using AES.Tools.TBC.Collections;
using UnityEngine;


namespace ParkingCat.Game
{
    public class ForceExitDimmer : ContextListenerBase
    {
        [SerializeField] GameObject dimmer;

        CompositeDisposable _disposable;
        

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            var vm = ctx.GetValue() as GameViewModel;
            if (vm == null) return;
            _disposable = new CompositeDisposable();

            vm.CurrentBoosterMode.Subscribe(m =>
            {
                if (m == GameViewModel.BoosterMode.ForceExit)
                    dimmer.SetActive(true);
                else
                    dimmer.SetActive(false);
            }).AddTo(_disposable);
        }

        protected override void OnContextUnavailable()
        {
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}