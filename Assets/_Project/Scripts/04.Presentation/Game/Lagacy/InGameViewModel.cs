// // ParkingCat.Presentation/InGameViewModel.cs
// using AES.Tools;
// using AES.Tools.StateMachine.Core;
// using Application;
// using Cysharp.Threading.Tasks;
// using Domain.Sim;
// using ParkingCat.Application;
//
// namespace ParkingCat.Presentation
// {
//     public sealed class InGameViewModel
//     {
//         public ObservableProperty<int> RemainingCars { get; }
//         public ObservableProperty<int> MovesUsed { get; }
//         public ObservableProperty<RunStatus> Status { get; }
//         public ObservableProperty<bool> IsInputLocked { get; }
//
//         // StateMachine outputs (바인딩용) :
//         public ObservableProperty<IState> CurrentState => _sm.CurrentState;
//         public ObservableProperty<string> LastTransitionName => _sm.LastTransitionName;
//
//         public AsyncCommand TryMoveCommand { get; }
//
//         // 전이용 플래그(ObservablePredicate가 직접 읽음)
//         private readonly ObservableProperty<bool> _isAnimating = new();
//         private readonly ObservableProperty<bool> _isWin = new();
//
//         private readonly StateMachine _sm = new();
//         public StateMachine Machine => _sm;
//         public ObservableList<CarItemViewModel> Cars { get; set; } = new();
//
//         private readonly InGameIdleState _idle = new();
//         private readonly InGameAnimatingState _anim = new();
//         private readonly InGameWinState _win = new();
//
//         private readonly TryMoveUseCase _tryMove;
//         private readonly GameProgressUseCase _game;
//
//         public InGameViewModel(
//             TryMoveUseCase tryMove,
//             GameProgressUseCase game)
//         {
//             _tryMove = tryMove;
//             _game = game;
//
//             RemainingCars = new ObservableProperty<int>(_game.RemainingCars);
//             MovesUsed = new ObservableProperty<int>(_game.MovesUsed);
//             Status = new ObservableProperty<RunStatus>(_game.Status);
//
//             InGameStateGraph.Build(_sm, _idle, _anim, _win, _isAnimating, _isWin);
//
//             TryMoveCommand = new AsyncCommand(
//                 executeAsync: async param =>
//                 {
//                     if (param is not int carId) return;
//                     if (_game.Status != RunStatus.Running) return;
//
//                     _isAnimating.Value = true; // Idle -> Anim
//
//                     var result = _tryMove.Execute(carId);
//
//                     // Commit은 기다리지 않는다(의도 유지)
//                     var needWait = result.Decision != SimDecision.Commit;
//
//                     UniTask waitTask = UniTask.CompletedTask;
//                     UniTaskCompletionSource tcs = null;
//
//                     if (needWait)
//                     {
//                         tcs = new UniTaskCompletionSource();
//                         waitTask = tcs.Task;
//                     }
//
//                     // Rollback/Rejected만 끝났을 때 tcs 완료
//                     if (needWait)
//                         await waitTask;
//
//                     Sync();
//
//                     _isAnimating.Value = false; // Anim -> Idle (또는 Any->Win)
//                     _isWin.Value = (Status.Value == RunStatus.Win);
//
//                     TryMoveCommand?.RaiseCanExecuteUpdated();
//                 },
//                 canExecute: _ => _game.Status == RunStatus.Running
//             );
//         }
//
//         public void Tick(float now)
//         {
//             _sm.Update(now);
//         }
//
//         private void Sync()
//         {
//             RemainingCars.Value = _game.RemainingCars;
//             MovesUsed.Value = _game.MovesUsed;
//             Status.Value = _game.Status;
//         }
//     }
// }
