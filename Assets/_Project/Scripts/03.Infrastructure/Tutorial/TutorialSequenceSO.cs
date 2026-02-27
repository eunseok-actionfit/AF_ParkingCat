using System;
using System.Collections.Generic;
using UnityEngine;
using ParkingCat.Entities;

[CreateAssetMenu(menuName = "ParkingCat/Tutorial/TutorialSequence", fileName = "TutorialSequence_Stage")]
public sealed class TutorialSequenceSO : ScriptableObject
{
    // 튜토리얼이 속한 스테이지 식별자
    [Tooltip("이 튜토리얼 시퀀스가 적용되는 스테이지 ID")]
    public int stageId = 1;

    // 스테이지 내에서 순차적으로 실행되는 튜토리얼 단계 목록
    [Tooltip("튜토리얼을 구성하는 단계 목록")]
    public List<TutorialStep> steps = new();

    /// <summary>
    /// 튜토리얼의 단일 진행 단계를 정의한다.<br/>
    /// 메시지 표시, 대상 선택, 입력 제한을 함께 묶는다.
    /// </summary>
    /// <remarks>
    /// 순서대로 실행되며 성공 조건을 만족하면 다음 단계로 진행된다.<br/>
    /// 각 단계는 지연 시간과 입력 제어 정책을 개별적으로 가진다.
    /// </remarks>
    [Serializable]
    public sealed class TutorialStep
    {
        // UI에 표시할 메시지 키
        [Tooltip("튜토리얼 단계에서 표시할 UI 메시지 키")]
        public string messageKey;

        // 이 단계에서 강조하거나 조작을 요구하는 대상 선택 규칙
        [Tooltip("튜토리얼 대상 엔티티를 선택하는 기준")]
        public TutorialTargetSelector target;

        // 대상 외의 모든 입력을 잠글지 여부
        [Tooltip("튜토리얼 대상 외의 모든 입력을 잠글지 여부")]
        public bool lockAllOtherInputs = true;

        // 단계 표시 전 대기 시간(초)
        [Tooltip("단계 시작 전 대기 시간(초)")]
        public float delayBeforeShow = 0.0f;

        // 성공 처리 후 다음 단계로 넘어가기 전 대기 시간(초)
        [Tooltip("단계 성공 후 대기 시간(초)")]
        public float delayAfterSuccess = 0.15f;
    }

    /// <summary>
    /// 튜토리얼 단계에서 사용할 대상 선택 방식을 정의한다.<br/>
    /// 차량을 기준으로 다양한 선택 규칙을 제공한다.
    /// </summary>
    /// <remarks>
    /// 모드에 따라 서로 다른 필드 조합이 사용된다.<br/>
    /// 선택되지 않는 필드는 무시된다.
    /// </remarks>
    [Serializable]
    public sealed class TutorialTargetSelector
    {
        /// <summary>
        /// 튜토리얼 대상 선택 기준을 정의한다.
        /// </summary>
        public enum Mode
        {
            ByCarTypeNth,
            ByCarsListIndex,
            ByExplicitCarId
        }

        // 대상 선택에 사용할 모드
        [Tooltip("튜토리얼 대상 선택 방식")]
        public Mode mode = Mode.ByCarTypeNth;

        // 특정 차량 타입 기준 선택 시 사용할 차량 타입
        [Tooltip("선택 기준이 되는 차량 타입")]
        public CarType carType;

        // 동일 타입 차량 중 몇 번째인지 지정
        [Tooltip("동일 타입 차량 중 선택할 순번")]
        public int nth = 0;

        // 차량 리스트 인덱스로 직접 선택
        [Tooltip("차량 리스트에서의 인덱스 값")]
        public int carsListIndex = 0;

        // 명시적 차량 ID로 직접 선택
        [Tooltip("직접 지정할 차량의 고유 ID")]
        public int explicitCarId = -1;
    }
}
