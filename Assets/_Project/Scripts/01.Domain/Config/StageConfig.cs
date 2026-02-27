using System;
using ParkingCat.Entities;
using ParkingCat.ValueObjects;
using ParkingCat.ValueObjects.Level;
using UnityEngine;

namespace ParkingCat.Config
{
    [Serializable]
    public sealed class StageConfig
    {
        public int StageId;
        public int Variant;

        public int Width;
        public int Height;

        /// <summary>-1이면 무한</summary>
        public int MoveLimit;
        public LevelDifficulty Difficulty;

        /// <summary>row-major (y*w + x), 1=road, 0=wall</summary>
        public int[] Road01;

        public CarConfig[] Cars;
        
        public TrafficLightConfig[] TrafficLights;
        
        public OneWayLaneConfig[] OneWayLanes;

        [Serializable]
        public sealed class CarConfig
        {
            public int CarId;
            public int Length;
            public Vector2Int HeadCell;

            public Dir4 Dir;
            public TurnAction[] TurnProgram;
    
            public CarType Type;
            public int EscapeMoveLimit;

            public int InitialCashGold; // CashTruck 전용 (0이면 무시)
        }

        
        [Serializable]
        public sealed class TrafficLightConfig
        {
            public int Id;

            // 신호등 아이콘 위치(월드). 에디터에서는 그리드 스냅으로 넣어줌.
            public Vector2 WorldPos;

            // 판정선 방향/길이
            public Dir4 LineDir;

            // 위상 오프셋(필요 시)
            public float PhaseOffsetSeconds;
        }
        
        [Serializable]
        public sealed class OneWayLaneConfig
        {
            // 밴드 시작 셀 (좌상단 기준)
            public Vector2Int StartCell;

            // 밴드 진행 방향
            public Dir4 Dir;

            // 연속 길이 (run)
            public int Length;

            // 밴드 두께 (= 차선 수)
            public int Thickness;

            // 대상 차선 인덱스 (0 .. Thickness-1)
            public int LaneIndex;

            // 허용 진행 방향
            public Dir4 AllowedDir;
        }

    }
}