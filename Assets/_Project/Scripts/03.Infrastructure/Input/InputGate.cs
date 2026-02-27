using ParkingCat.Move.Ports;


namespace ParkingCat.Input
{
    /// <summary>
    /// 입력 잠금 상태를 관리하는 기본 구현체다.<br/>
    /// 동시 입력 및 중복 실행을 방지한다.
    /// </summary>
    /// <remarks>
    /// 단순 플래그 기반으로 동작한다.<br/>
    /// 스레드 안전성은 호출자가 보장해야 한다.
    /// </remarks>
    public sealed class InputGate : IInputGate
    {
        // 현재 입력 잠금 상태
        public bool Locked { get; private set; }

        /// <summary>
        /// 입력을 잠금 상태로 전환한다.
        /// </summary>
        public void Lock() => Locked = true;

        /// <summary>
        /// 입력 잠금을 해제한다.
        /// </summary>
        public void Unlock() => Locked = false;
    }
}