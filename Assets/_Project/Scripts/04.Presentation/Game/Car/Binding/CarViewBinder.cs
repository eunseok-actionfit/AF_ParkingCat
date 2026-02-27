using AES.Tools;
using AES.Tools.TBC.Collections;
using UnityEngine;


namespace ParkingCat.Game.Car.Binding
{
    [RequireComponent(typeof(CarView))]
    public sealed class CarViewBinder : ContextListenerBase
    {
        [SerializeField] private Transform visualRoot;
        CarView _view;

        CompositeDisposable _disposable;

        void Awake()
        {
            _view = GetComponent<CarView>();
        }

        protected override void OnContextAvailable(IBindingContext ctx)
        {

            if (ctx.GetValue() is not CarItemViewModel vm)
                return;

            _disposable?.Dispose();
            _disposable = new CompositeDisposable();

            _view.RegisterToRuntime(vm.CarId, vm.TurnProgram.Value, vm.TurnCursor.Value);
            
#if LevelEditor
            vm.Pos.Subscribe(p =>
            {
                var tr = _view.transform;
                tr.position = new Vector3(p.x, p.y, tr.position.z);
            }).AddTo(_disposable);
            
                   vm.HeadingRad.Subscribe(rad =>
            {
                float deg = rad * Mathf.Rad2Deg;
                float z = deg - 90f;
            
                var vtr = visualRoot.transform;
                vtr.rotation = Quaternion.Euler(0f, 0f, z);
            }).AddTo(_disposable);
  #else
            var tr = _view.transform;
            var p = vm.Pos.Value;
            tr.position = new Vector3(p.x, p.y, tr.position.z);
            float deg = vm.HeadingRad.Value * Mathf.Rad2Deg;
            float z = deg - 90f;

            var vtr = visualRoot.transform;
            vtr.rotation = Quaternion.Euler(0f, 0f, z);
#endif



            vm.IsLeaving.Subscribe(isLeaving =>
            {
                if (isLeaving)
                    _view.MarkLeaving();
            }).AddTo(_disposable);


            // // Turn 관련이 바뀌면 registry에도 반영(권장)
            // vm.TurnCursor.Subscribe(c =>
            // {
            //     _view.RegisterToRuntime(vm.CarId, vm.TurnProgram.Value, c);
            // }).AddTo(_disposable);
            //
            // vm.TurnProgram.Subscribe(p =>
            // {
            //     _view.RegisterToRuntime(vm.CarId, p, vm.TurnCursor.Value);
            // }).AddTo(_disposable);

            vm.IsExitHinted.Subscribe(on =>
                _view.SetExitHint(on)).AddTo(_disposable);
        }

        protected override void OnContextUnavailable()
        {
            _disposable?.Dispose();
        }

    }
}