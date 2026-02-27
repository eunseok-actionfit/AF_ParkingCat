// namespace Application
// {
//     // /// <summary>
//     // /// 스테이지 진행 상태를 정의한다.<br/>
//     // /// 실행 중, 클리어, 실패를 구분한다.
//     // /// </summary>
//     // public enum StageOutcome
//     // {
//     //     Running,
//     //     Cleared,
//     //     Failed
//     // }
//     //
//     // /// <summary>
//     // /// 실패가 확정될 때의 원인을 정의한다.<br/>
//     // /// UI 메시지 및 리트라이 정책에 사용된다.
//     // /// </summary>
//     // public enum FailReason
//     // {
//     //     MoveLimitExhausted,
//     //     Gimmick
//     // }
//
//     /// <summary>
//     /// 스테이지 진행 상황을 추적하는 계약을 정의한다.<br/>
//     /// 이동 제한과 클리어/실패 조건을 관리한다.
//     /// </summary>
//     /// <remarks>
//     /// 이동 제한 실패는 애니메이션 종료 후 확정될 수 있다.<br/>
//     /// 구현은 상태 전이 규칙을 일관되게 유지해야 한다.
//     /// </remarks>
//     public interface IGameProgress
//     {
//         /// <summary>
//         /// 현재 스테이지 결과 상태를 반환한다.
//         /// </summary>
//         StageOutcome Outcome { get; }
//
//         /// <summary>
//         /// 실패 상태일 때의 실패 원인을 반환한다.
//         /// </summary>
//         FailReason? FailReason { get; }
//
//         /// <summary>
//         /// 남은 이동 횟수를 반환한다.<br/>
//         /// -1은 무제한을 의미한다.
//         /// </summary>
//         int RemainingMoves { get; }
//
//         /// <summary>
//         /// 남아있는 활성 차량 수를 반환한다.<br/>
//         /// 0이 되면 클리어 조건이 될 수 있다.
//         /// </summary>
//         int ActiveCars { get; }
//
//         /// <summary>
//         /// 스테이지 진행 상태를 초기화한다.
//         /// </summary>
//         /// <param name="initialActiveCars">초기 활성 차량 수</param>
//         /// <param name="moveLimit">이동 제한 값(-1은 무제한)</param>
//         void Reset(int initialActiveCars, int moveLimit);
//
//         /// <summary>
//         /// 입력이 수락된 경우에만 이동을 소비한다.<br/>
//         /// 남은 이동이 0이 되면 실패가 예약될 수 있다.
//         /// </summary>
//         /// <param name="consumed">이동을 소비할지 여부</param>
//         void ConsumeMoveIfNeeded(bool consumed);
//
//         /// <summary>
//         /// 차량이 커밋되어 탈출했을 때 호출한다.<br/>
//         /// 활성 차량 수를 갱신한다.
//         /// </summary>
//         void OnCarCommitted();
//
//         /// <summary>
//         /// 기믹으로 인해 즉시 실패를 트리거한다.<br/>
//         /// 충돌 즉시 실패 같은 규칙에 사용된다.
//         /// </summary>
//         void TriggerFailGimmick();
//
//         /// <summary>
//         /// 연출이 모두 끝난 뒤 호출한다.<br/>
//         /// 예약된 이동 제한 실패를 확정하는 데 사용된다.
//         /// </summary>
//         void OnAnimationSettled();
//     }
// }
