
using ParkingCat.Entities;
using ParkingCat.ValueObjects;
using UnityEngine;


namespace ParkingCat.World
{
    /// <summary>
    /// 시뮬레이터가 월드 상태를 질의하기 위한 계약을 정의한다.<br/>
    /// 경계, 도로, 출구, 충돌 정보를 제공한다.
    /// </summary>
    /// <remarks>
    /// 구현은 결정론적 결과를 위해 동일 입력에 동일 출력을 반환해야 한다.<br/>
    /// 충돌 질의는 연출용 파라미터를 함께 제공한다.
    /// </remarks>
    public interface IWorldQuery
    {
        /// <summary>
        /// 지정 포즈가 월드 경계 안에 있는지 확인한다.
        /// </summary>
        /// <param name="pose">검사할 포즈</param>
        /// <returns>경계 안이면 <c>true</c>, 아니면 <c>false</c></returns>
        bool InBounds(in Pose2 pose);

        /// <summary>
        /// 지정 포즈가 도로 위에 있는지 확인한다.
        /// </summary>
        /// <param name="pose">검사할 포즈</param>
        /// <returns>도로면 <c>true</c>, 아니면 <c>false</c></returns>
        bool IsRoad(in Pose2 pose);
        
        bool IsEntryAllowed(in Pose2 pose, float headingRad);

        /// <summary>
        /// 이동체가 충돌했는지 검사하고 연출용 충돌 정보를 반환한다.<br/>
        /// 충돌이 없으면 `CollisionHit.None`을 반환한다.
        /// </summary>
        /// <param name="moverId">이동체 ID</param>
        /// <param name="moverPose">이동체 포즈</param>
        /// <param name="moverDims">이동체 치수</param>
        /// <param name="moverSpeed">이동체 속도</param>
        /// <returns>충돌 결과</returns>
        CollisionHit CheckCollision(int moverId, in Pose2 moverPose, in CarDims moverDims, float moverSpeed);
    }

    /// <summary>
    /// 충돌 발생 여부와 피해 대상, 연출 파라미터를 담는다.<br/>
    /// 도메인 로직이 표현 레이어와 분리되도록 설계된다.
    /// </summary>
    /// <remarks>
    /// `ImpactDir`은 단위 벡터를 기대한다.<br/>
    /// `Strength`는 0..1 범위를 권장한다.
    /// </remarks>
    public readonly struct CollisionHit
    {
        // 충돌 발생 여부
        public readonly bool Hit;

        // 피해 차량 ID(벽 충돌은 -1)
        public readonly int VictimCarId;

        // 충돌 사유 식별자
        public readonly string Reason;

        // 충돌 방향 단위 벡터
        public readonly Vector2 ImpactDir;

        // 연출용 충돌 강도(0..1 권장)
        public readonly float Strength;

        /// <summary>
        /// 충돌 결과를 생성한다.
        /// </summary>
        /// <param name="hit">충돌 여부</param>
        /// <param name="victimCarId">피해 차량 ID</param>
        /// <param name="reason">충돌 사유</param>
        /// <param name="impactDir">충돌 방향</param>
        /// <param name="strength">충돌 강도</param>
        public CollisionHit(bool hit, int victimCarId, string reason, Vector2 impactDir, float strength)
        {
            Hit = hit;
            VictimCarId = victimCarId;
            Reason = reason;
            ImpactDir = impactDir;
            Strength = strength;
        }

        /// <summary>
        /// 충돌이 없는 상태를 나타낸다.
        /// </summary>
        public static CollisionHit None => new(false, -1, "", Vector2.zero, 0f);
    }
}
