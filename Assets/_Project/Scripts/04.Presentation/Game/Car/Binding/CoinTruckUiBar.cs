using AES.Tools;
using AES.Tools.TBC.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace ParkingCat.Game.Car.Binding
{
    public class CoinTruckUiBar : ContextListenerBase
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text coinCount;
        
        private CompositeDisposable _disposable;
        private Tween _fillTween;
        protected override void OnContextAvailable(IBindingContext ctx)
        {
            var vm = ctx.GetValue() as CarItemViewModel;
            if (vm == null) return;
            _disposable = new CompositeDisposable();

            vm.CashGold.Subscribe(coin => coinCount.text = coin.ToString()).AddTo(_disposable);
            vm.CashCountdown01
                .Subscribe(p =>
                {
                    float target = 1f - p;

                    _fillTween?.Kill();
                    _fillTween = fill
                        .DOFillAmount(target, 0.15f)
                        .SetEase(Ease.OutCubic);
                })
                .AddTo(_disposable);
        }
        protected override void OnContextUnavailable()
        {
            _disposable?.Dispose();
            _disposable = null;
            _fillTween?.Kill();
            _fillTween = null;
        }
    }
}


