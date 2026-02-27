


using UnityEngine;


namespace ParkingCat.ValueObjects
{
    /// <summary>
    /// 2차원 공간에서의 위치와 방향을 함께 표현한다.<br/>
    /// 시뮬레이션 및 궤적 기록의 기본 단위로 사용된다.
    /// </summary>
    /// <remarks>
    /// 불변 구조체로 설계되어 값 전달에 적합하다.<br/>
    /// 좌표와 각도의 일관성은 호출자가 보장해야 한다.
    /// </remarks>
    public readonly struct Pose2
    {
        // 월드 좌표계 기준 위치
        public readonly Vector2 Pos;

        // 진행 방향 각도(라디안)
        public readonly float HeadingRad;

        /// <summary>
        /// 위치와 방향으로 포즈를 생성한다.
        /// </summary>
        /// <param name="pos">월드 좌표계 위치</param>
        /// <param name="headingRad">방향 각도(라디안)</param>
        public Pose2(Vector2 pos, float headingRad)
        {
            Pos = pos;
            HeadingRad = headingRad;
        }
    }
}