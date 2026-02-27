namespace ParkingCat.ValueObjects
{
    /// <summary>
    /// 교차로에서 선택 가능한 회전 동작을 정의한다.<br/>
    /// 시뮬레이션의 분기 의도를 표현한다.
    /// </summary>
    public enum TurnAction
    {
        Straight,
        Left,
        Right
    }

    /// <summary>
    /// 차량이 순차적으로 수행할 회전 명령 집합을 관리한다.<br/>
    /// 커서 기반으로 다음 회전 동작을 제공한다.
    /// </summary>
    /// <remarks>
    /// 루프 여부에 따라 명령 시퀀스가 반복될 수 있다.<br/>
    /// 커서 범위 초과 시 기본 동작은 Straight로 처리된다.
    /// </remarks>
    public sealed class TurnProgram
    {
        // 순서대로 수행할 회전 동작 목록
        public TurnAction[] Actions = System.Array.Empty<TurnAction>();

        // 마지막 동작 이후 처음으로 되돌릴지 여부
        public bool Loop = false;

        /// <summary>
        /// 등록된 회전 동작 개수를 반환한다.
        /// </summary>
        public int Length => Actions?.Length ?? 0;

        /// <summary>
        /// 현재 커서 위치에서 수행할 회전 동작을 조회한다.<br/>
        /// 커서는 증가하지 않는다.
        /// </summary>
        /// <param name="cursor">현재 회전 커서</param>
        /// <returns>다음에 수행할 회전 동작</returns>
        public TurnAction Peek(int cursor)
        {
            if (Length <= 0) return TurnAction.Straight;
            if (!Loop && cursor >= Length) return TurnAction.Straight;

            if (Loop)
                cursor %= Length;

            if (cursor < 0) cursor = 0;
            if (cursor >= Length) cursor = Length - 1;

            return Actions[cursor];
        }

        /// <summary>
        /// 회전 동작을 하나 소비한 뒤 다음 커서를 반환한다.
        /// </summary>
        /// <param name="cursor">현재 회전 커서</param>
        /// <returns>다음 회전 커서</returns>
        public int Consume(int cursor)
        {
            if (Length <= 0) return cursor;
            if (Loop) return (cursor + 1) % Length;
            return cursor + 1;
        }
    }
    
    public static class TurnPresets
    {
        public readonly static TurnAction[] Left     = { TurnAction.Left };
        public readonly static TurnAction[] Right    = { TurnAction.Right };

        // U-turn macros
        public readonly static TurnAction[] UTurnLeft  = { TurnAction.Left, TurnAction.Left };
        public readonly static TurnAction[] UTurnRight = { TurnAction.Right, TurnAction.Right };
        
        public readonly static TurnAction[] LeftRight = { TurnAction.Left, TurnAction.Right };
        public readonly static TurnAction[] RightLeft = { TurnAction.Right, TurnAction.Left };
    }
}