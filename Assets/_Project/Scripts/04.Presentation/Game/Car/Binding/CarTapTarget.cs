using AES.Tools;

namespace ParkingCat.Game.Car.Binding
{
    public sealed class CarTapTarget : ContextListenerBase
    {
        CarItemViewModel _vm;
        public int CarIndex;

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            _vm = ctx.GetValue() as CarItemViewModel;
            if (_vm == null) return;

            CarIndex = _vm.CarId;
        }

        protected override void OnContextUnavailable()
        {
            _vm = null;
        }

        public void TryExecuteMove()
        {
            // CarItemViewModel이 GameViewModel 참조를 이미 가지고 있으므로(_vm 생성 시 this 전달)
            _vm?.Owner?.OnCarTappedCommand.Execute(_vm.CarId);
        }
    }
}