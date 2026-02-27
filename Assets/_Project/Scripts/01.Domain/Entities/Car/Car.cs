

using UnityEngine;


namespace ParkingCat.Entities
{
    /// <summary>
    /// 차량의 런타임 상태(Active/Leaving)를 정의한다.
    /// - Leaving: Commit(탈출) 처리되어 시뮬/충돌 대상에서 제외될 수 있음
    /// </summary>
    public enum CarRuntimeState
    {
        Active,
        Leaving
    }
    
    public enum CarType
    {
        Normal,
        Ambulance,
        CashTruck
    }

    /// <summary>
    /// 차량의 현재 물리 상태와 주행 의도를 표현한다.<br/>
    /// 시뮬레이션 및 의사결정 로직의 기준 데이터로 사용된다.
    /// </summary>
    /// <remarks>
    /// 위치, 방향, 속도와 함께 회전 프로그램 상태를 포함한다.<br/>
    /// 프레임 단위로 갱신되며 불변성을 보장하지 않는다.
    /// </remarks>
    public sealed class Car
    {
        // 차량을 식별하기 위한 고유 ID
        public int Id;
        
        public CarType Type = CarType.Normal;

        public int RemainingEscapeMoves = -1; 
        public bool IsAmbulance => Type == CarType.Ambulance;
        public bool IsCashTruck => Type == CarType.CashTruck;
        
        public int CashGold;                 // 현재 보유 골드
        public float CashDecayElapsed;       // 마지막 차감 이후 경과시간(초)

        // 월드 좌표계 기준 차량 중심 위치
        public Vector2 Pos;

        // 차량의 진행 방향 (라디안)
        public float HeadingRad;

        // 현재 속도 (m/s, 전진 + / 후진 -)
        public float Speed;

        // 차량의 물리적 크기 정보
        public CarDims Dims = new(3.8f, 1.7f);

        // 회전 프로그램에서 현재 실행 중인 단계 인덱스
        public int TurnCursor;

        // 차량 회전을 정의하는 프로그램 데이터
        public ValueObjects.TurnProgram TurnProgram = new();

        // 런타임 상태 (Commit 이후 Leaving으로 바뀜)
        public CarRuntimeState RuntimeState = CarRuntimeState.Active;

        // Stage/Simulator에서 편하게 쓰는 플래그
        public bool IsLeaving => RuntimeState == CarRuntimeState.Leaving;

        public void MarkLeaving() => RuntimeState = CarRuntimeState.Leaving;
    }

    /// <summary>
    /// 차량의 외형 치수를 정의한다.<br/>
    /// 충돌 판정 및 공간 계산에 사용된다.
    /// </summary>
    public readonly struct CarDims
    {
        // 차량의 전체 길이 (미터)
        public readonly float Length;

        // 차량의 전체 폭 (미터)
        public readonly float Width;

        public CarDims(float length, float width)
        {
            Length = length;
            Width = width;
        }
    }
}