using System.Collections.Generic;
using ParkingCat.ValueObjects;
using UnityEngine;


namespace ParkingCat.Sim
{
    /// <summary>
    /// 시뮬레이션 결과에 대한 최종 판단 상태를 정의한다.<br/>
    /// 외부 시스템이 결과를 처리하는 기준으로 사용된다.
    /// </summary>
    public enum SimDecision
    {
        Commit,
        Rollback,
        Rejected
    }

    /// <summary>
    /// 시뮬레이션 중 발생할 수 있는 이벤트 유형을 정의한다.<br/>
    /// 결과 분석 및 후처리에 사용된다.
    /// </summary>
    public enum SimEventType
    {
        JunctionTurnConsumed,
        Collision,
        Exit
    }

    /// <summary>
    /// 시뮬레이션 중 단일 프레임에서 발생한 사건을 표현한다.<br/>
    /// 충돌, 회전 소모, 종료 사유 등을 기록한다.
    /// </summary>
    /// <remarks>
    /// 이벤트 유형에 따라 일부 필드만 의미를 가진다.<br/>
    /// 사용자는 Type을 기준으로 해석해야 한다.
    /// </remarks>
    public readonly struct SimEvent
    {
        // 이벤트의 종류
        public readonly SimEventType Type;

        // 이벤트가 발생한 프레임 인덱스
        public readonly int FrameIndex;

        // 교차로 회전 소모 시 사용되는 회전 액션
        public readonly TurnAction Turn;

        // 충돌 시 피해 차량 ID
        public readonly int VictimCarId;

        // 충돌 방향 벡터
        public readonly Vector2 ImpactDir;

        // 충돌 강도 값
        public readonly float Strength;

        // 이벤트 발생 사유 또는 설명
        public readonly string Reason;

        /// <summary>
        /// 시뮬레이션 이벤트 데이터를 생성한다.
        /// </summary>
        /// <param name="type">이벤트 유형</param>
        /// <param name="frameIndex">발생 프레임</param>
        /// <param name="turn">회전 액션 정보</param>
        /// <param name="victimCarId">피해 차량 ID</param>
        /// <param name="impactDir">충돌 방향</param>
        /// <param name="strength">충돌 강도</param>
        /// <param name="reason">발생 사유</param>
        public SimEvent(
            SimEventType type,
            int frameIndex,
            TurnAction turn,
            int victimCarId,
            Vector2 impactDir,
            float strength,
            string reason)
        {
            Type = type;
            FrameIndex = frameIndex;
            Turn = turn;
            VictimCarId = victimCarId;
            ImpactDir = impactDir;
            Strength = strength;
            Reason = reason;
        }
    }

    /// <summary>
    /// 시간 순서에 따른 차량 궤적을 저장한다.<br/>
    /// 시뮬레이션 결과 재생에 사용된다.
    /// </summary>
    public sealed class Trajectory
    {
        // 프레임별 차량 포즈 목록
        public readonly List<Pose2> Poses = new();

        /// <summary>
        /// 저장된 포즈 개수를 반환한다.
        /// </summary>
        public int Count => Poses.Count;
    }

    /// <summary>
    /// 단일 시뮬레이션 실행 결과를 표현한다.<br/>
    /// 판단 결과, 궤적, 이벤트 정보를 포함한다.
    /// </summary>
    /// <remarks>
    /// 충돌 정보는 빠른 접근을 위해 요약 필드로도 제공된다.
    /// </remarks>
    public sealed class SimResult
    {
        // 시뮬레이션에 대한 최종 판단
        public SimDecision Decision;

        // 판단에 대한 설명 또는 거절 사유
        public string Reason = "";

        // 정상 진행 시의 차량 궤적
        public readonly Trajectory Forward = new();

        // 롤백 시의 차량 궤적
        public readonly Trajectory Rollback = new();

        // 시뮬레이션 중 발생한 모든 이벤트
        public readonly List<SimEvent> Events = new();

        // 최초 충돌이 발생한 프레임 인덱스
        public int HitFrameIndex = -1;

        // 충돌 피해 차량 ID
        public int VictimCarId = -1;

        // 충돌 방향 벡터
        public Vector2 ImpactDir = Vector2.zero;

        // 충돌 강도 요약 값
        public float ImpactStrength = 0f;
    }
}
