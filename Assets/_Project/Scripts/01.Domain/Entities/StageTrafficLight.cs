using ParkingCat.ValueObjects;
using UnityEngine;

namespace ParkingCat.Entities
{
    /// <summary>스테이지에 고정 배치되는 신호등 설정(정적).</summary>
    public sealed class StageTrafficLight
    {
        public int Id { get; }
        public Vector2 WorldPos { get; }
        public Dir4 LineDir { get; }
        public float PhaseOffsetSeconds { get; }

        public StageTrafficLight(int id, Vector2 worldPos, Dir4 lineDir, float phaseOffsetSeconds)
        {
            Id = id;
            WorldPos = worldPos;
            LineDir = lineDir;
            PhaseOffsetSeconds = Mathf.Max(0f, phaseOffsetSeconds);
        }
    }
}