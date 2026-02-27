using System;
using UnityEngine;

namespace ParkingCat.Sim
{
    /// <summary>
    /// 시뮬레이션 실행에 필요한 파라미터 모음을 정의한다.<br/>
    /// 재현 가능한 결과를 위해 고정 값으로 운용된다.
    /// </summary>
    /// <remarks>
    /// 실제 차량 물리 대신 퍼즐 친화적 이동을 전제로 한다.<br/>
    /// 단위와 허용 범위를 코드 사용자가 일관되게 유지해야 한다.
    /// </remarks>
    [Serializable]
    public sealed class SimulationSettings
    {
        [Tooltip("프레임당 시뮬레이션 시간 간격(초)")]
        [SerializeField] public float Dt = 1f / 60f;

        [Tooltip("시뮬레이션을 중단하는 최대 누적 시간(초)")]
        [SerializeField] public float MaxSeconds = 5f;

        [Tooltip("직진 이동 시 적용되는 고정 속도(m/s)")]
        [SerializeField] public float ForwardSpeed = 4.0f;

        [Tooltip("측면 도로 폭을 측정할 샘플 간격")]
        [SerializeField] public float SideSampleStep = 0.2f;

        [Tooltip("측면 도로 탐색의 최대 거리")]
        [SerializeField] public float SideMaxReach = 3.0f;

        [Tooltip("교차로 폭 변화로 판단하는 최소 차이값")]
        [SerializeField] public float WidthDeltaThreshold = 0.25f;

        [Tooltip("90도 회전에 사용되는 회전 반경")]
        [SerializeField] public float TurnRadius = 0.8f;

        [Tooltip("회전 진행 속도(거리/초 기준)")]
        [SerializeField] public float TurnSpeed = 3.0f;

        [Tooltip("충돌 강도 1.0에 대응하는 기준 속도")]
        [SerializeField] public float ImpactSpeedForMax = 6.0f;

        [Tooltip("교차로 감지를 위한 전방 탐색 거리")]
        [SerializeField] public float JunctionLookAhead = 0.35f;

        [Tooltip("분기 개방 여부를 판단하는 최소 폭 변화값")]
        [SerializeField] public float BranchDeltaThreshold = 0.25f;

        [Tooltip("전방 분기 유효성 확인을 위한 추가 탐색 거리")]
        [SerializeField] public float BranchProbe = 0.55f;

        // (추가)
        [Tooltip("턴 토큰 소비 직후, 이 거리만큼 이동하기 전까지는 다음 토큰 소비 체크를 하지 않음(월드 유닛)")]
        [SerializeField] public float JunctionConsumeCooldownDist = 1.0f;
    }
}