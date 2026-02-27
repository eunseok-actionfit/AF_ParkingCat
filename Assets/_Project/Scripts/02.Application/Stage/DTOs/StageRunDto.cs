using System;
using ParkingCat.Entities;
using ParkingCat.ValueObjects;
using ParkingCat.ValueObjects.Level;
using UnityEngine;


namespace ParkingCat.Stage.DTOs
{
    public sealed class StageRunDto
    {
        public int StageId;
        public int Variant;

        public int Width;
        public int Height;
        
        // 0/1 grid [y,x]
        public int[,] Grid01;

        public int RemainingMoves;
        public StageStatus Status;
        public LevelDifficulty Difficulty;
        
        public int PendingCoin;
        public int EscapedCount;


        public int ActiveCarCount;

        public CarDto[] Cars = Array.Empty<CarDto>();
        public TrafficLightDto[] TrafficLights = Array.Empty<TrafficLightDto>();

        [Serializable]
        public sealed class TrafficLightDto
        {
            public int Id;
            public Vector2 WorldPos;
            public Dir4 LineDir;
            public int LineLength;
            public float PhaseOffsetSeconds;

            public TrafficLightState State;
            public int RemainSeconds;
        }

        [Serializable]
        public sealed class CarDto
        {
            public int Id;
            public Vector2 Pos;
            public float HeadingRad;
            public CarType Type;
            public int RemainingEscapeMoves;

            public CarRuntimeState RuntimeState;

            public int TurnCursor;
            public bool TurnLoop;
            public TurnAction[] TurnActions = Array.Empty<TurnAction>();
            
            public int CashGold;
            public float CashCountdown01; // 0~1 (다음 차감까지 진행도)

        }
        
        public OneWayLaneDto[] OneWayLanes = Array.Empty<OneWayLaneDto>();

        [Serializable]
        public sealed class OneWayLaneDto
        {
            public Vector2Int StartCell;
            public Dir4 Dir;
            public int Length;
            public int Thickness;
            public int LaneIndex;
            public Dir4 AllowedDir;
        }
    }
}