// using AES.Tools.StateMachine.Core;
// using UnityEngine;
//
// namespace ParkingCat.Presentation
// {
//     public sealed class InGameStateMachineDriver : MonoBehaviour, IStateMachineOwner
//     {
//         private InGameViewModel _vm;
//
//         public void Bind(InGameViewModel vm) => _vm = vm;
//
//         private void Update()
//         {
//             if (_vm == null) return;
//             _vm.Tick(Time.time);
//         }
//
//         public StateMachine Machine => _vm.Machine;
//     }
// }