using AES.Tools;
using AES.Tools.TBC.Collections;
using ParkingCat.Entities;
using UnityEngine;

namespace ParkingCat.Game.TrafficLight
{
    [RequireComponent(typeof(TrafficLightView))]
    public sealed class TrafficLightViewBinder : ContextListenerBase
    {
        TrafficLightView _view;
        CompositeDisposable _disposable;

        void Awake()
        {
            _view = GetComponent<TrafficLightView>();
        }

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            if (ctx.GetValue() is not TrafficLightItemViewModel vm)
                return;

            _disposable?.Dispose();
            _disposable = new CompositeDisposable();

         
            vm.WorldPos.Subscribe(p =>
            {
                _view.SetWorldPos(p);
            }).AddTo(_disposable);

            vm.State.Subscribe(s =>
            {
                _view.SetLamp(s);
                _view.SetStopColliderEnabled(s == TrafficLightState.Red);
            }).AddTo(_disposable);

            vm.DisplayText.Subscribe(txt =>
            {
                _view.SetDisplayText(txt);
            }).AddTo(_disposable);
            

            vm.LineDir.Subscribe(dir =>
            {
                _view.SetDirection(dir);
            }).AddTo(_disposable);
            
        }

        protected override void OnContextUnavailable()
        {
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}
