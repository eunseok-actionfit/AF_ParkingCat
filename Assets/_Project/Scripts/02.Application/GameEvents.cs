namespace ParkingCat
{
    // TryStartMoveUseCase -> GameProgressUseCase 로 흘러가는 이벤트 포트
    public interface IGameEvents
    {
        // "실제로 움직임이 시작"된 순간(카운트 증가 타이밍)
     //   void OnMoveStarted(int carId, PlanType type);

        // Commit 정책이 "즉시 점유 제거/레포 제거"라서, Commit이면 시작과 동시에 Exit로 처리 가능
        void OnCarExited(int carId);

        // 입력이 막힘(soft conflict / global lock / 즉시막힘 등)
        void OnMoveRejected(int carId, string reason);

        // 추후 기믹용: 즉시 실패 트리거
        void OnFailTriggered(string reason);
    }
}