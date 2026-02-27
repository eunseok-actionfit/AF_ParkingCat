// using System;
//
// namespace Application
// {
//     /// <summary>
//     /// 스테이지 진행 상태를 추적하고 결과를 판정한다.<br/>
//     /// 이동 제한과 클리어/실패 규칙을 일관되게 적용한다.
//     /// </summary>
//     /// <remarks>
//     /// 이동 제한 실패는 연출 종료 후 확정된다.<br/>
//     /// 클리어 조건은 항상 실패 조건보다 우선한다.
//     /// </remarks>
//     public sealed class GameProgressUseCase : IGameProgress
//     {
//         // 현재 스테이지 결과 상태
//         public StageOutcome Outcome { get; private set; } = StageOutcome.Running;
//
//         // 실패 상태일 때의 실패 원인
//         public FailReason? FailReason { get; private set; }
//
//         // 남은 이동 횟수(-1은 무제한)
//         public int RemainingMoves { get; private set; } = -1;
//
//         // 남아있는 활성 차량 수
//         public int ActiveCars { get; private set; } = 0;
//
//         // 이동 제한 실패를 연출 종료까지 지연하기 위한 플래그
//         private bool _pendingFailByMoveLimit;
//
//         /// <summary>
//         /// 스테이지 진행 상태를 초기화한다.<br/>
//         /// 시작 상태가 이미 종료 조건이면 즉시 정규화한다.
//         /// </summary>
//         /// <param name="initialActiveCars">초기 활성 차량 수</param>
//         /// <param name="moveLimit">이동 제한 값(-1은 무제한)</param>
//         public void Reset(int initialActiveCars, int moveLimit)
//         {
//             Outcome = StageOutcome.Running;
//             FailReason = null;
//
//             ActiveCars = Math.Max(0, initialActiveCars);
//
//             RemainingMoves = moveLimit < 0 ? -1 : Math.Max(0, moveLimit);
//
//             _pendingFailByMoveLimit = false;
//
//             if (ActiveCars == 0)
//             {
//                 Outcome = StageOutcome.Cleared;
//                 return;
//             }
//
//             if (RemainingMoves == 0)
//             {
//                 _pendingFailByMoveLimit = true;
//             }
//         }
//
//         /// <summary>
//         /// 입력이 수락된 경우에만 이동을 소비한다.<br/>
//         /// 이동 제한이 0이 되면 실패를 예약한다.
//         /// </summary>
//         /// <param name="consumed">입력이 수락되어 이동을 소비했는지 여부</param>
//         public void ConsumeMoveIfNeeded(bool consumed)
//         {
//             if (Outcome != StageOutcome.Running) return;
//             if (!consumed) return;
//
//             if (RemainingMoves < 0) return;
//
//             if (RemainingMoves == 0)
//             {
//                 _pendingFailByMoveLimit = true;
//                 return;
//             }
//
//             RemainingMoves = Math.Max(0, RemainingMoves - 1);
//
//             if (RemainingMoves == 0)
//                 _pendingFailByMoveLimit = true;
//         }
//
//         /// <summary>
//         /// 차량이 커밋되어 탈출했을 때 진행 상태를 갱신한다.<br/>
//         /// 활성 차량이 0이면 클리어로 전이한다.
//         /// </summary>
//         public void OnCarCommitted()
//         {
//             if (Outcome != StageOutcome.Running) return;
//
//             ActiveCars = Math.Max(0, ActiveCars - 1);
//
//             if (ActiveCars == 0)
//             {
//                 Outcome = StageOutcome.Cleared;
//                 FailReason = null;
//             }
//         }
//
//         /// <summary>
//         /// 기믹에 의한 즉시 실패를 확정한다.
//         /// </summary>
//         public void TriggerFailGimmick()
//         {
//             if (Outcome != StageOutcome.Running) return;
//
//             Outcome = StageOutcome.Failed;
//             FailReason = Application.FailReason.GimmickTriggered;
//         }
//
//         /// <summary>
//         /// 모든 연출이 종료된 시점에 예약된 판정을 확정한다.<br/>
//         /// 클리어가 성립하면 실패 예약은 무시된다.
//         /// </summary>
//         public void OnAnimationSettled()
//         {
//             if (Outcome != StageOutcome.Running) return;
//
//             if (ActiveCars == 0)
//             {
//                 Outcome = StageOutcome.Cleared;
//                 FailReason = null;
//                 return;
//             }
//
//             if (_pendingFailByMoveLimit && RemainingMoves == 0)
//             {
//                 Outcome = StageOutcome.Failed;
//                 FailReason = Application.FailReason.MoveLimitExhausted;
//             }
//         }
//     }
// }
