using AES.Tools;
using ParkingCat.Game.Car.Binding;
using UnityEngine;


namespace ParkingCat.Game.Input
{
    public sealed class InputController : ContextListenerBase
    {
        [SerializeField] InputReader inputReader;
        [SerializeField] LayerMask targetLayer;

        GameViewModel _vm;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (inputReader == null) return;
            inputReader.EnablePlayerActions();
            inputReader.OnPointerDown += OnTap;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (inputReader == null) return;
            inputReader.OnPointerDown -= OnTap;
            inputReader.DisablePlayerActions();
        }

        void OnTap(Vector2 worldPos)
        {
            var hit = Physics2D.OverlapPoint(worldPos, targetLayer);

            if (hit == null)
            {
                _vm.OnCarTappedCommand.Execute(-1);
                return;
            }
            var tap = hit.GetComponentInParent<CarTapTarget>();

            if (tap == null)
            {
                _vm.OnCarTappedCommand.Execute(-1);
                return;
            }
            _vm.OnCarTappedCommand.Execute(tap.CarIndex);
        }


        protected override void OnContextAvailable(IBindingContext ctx)
        {
            _vm = ctx.GetValue() as GameViewModel;
            if (_vm == null) return;
        }

        protected override void OnContextUnavailable()
        {
            _vm = null;
        }
    }
}