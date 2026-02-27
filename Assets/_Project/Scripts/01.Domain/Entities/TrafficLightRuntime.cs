using UnityEngine;

namespace ParkingCat.Entities
{
    public enum TrafficLightState { Red, Green }

    /// <summary>진행 중인 신호등 상태(동적).</summary>
    public sealed class TrafficLightRuntime
    {
        public int Id { get; }
        public Vector2 WorldPos { get; }
        public Dir4 LineDir { get; }
        public float PhaseOffsetSeconds { get; }

        public TrafficLightState State { get; private set; } = TrafficLightState.Red;
        public int RemainSeconds { get; private set; } = 0;

        float _elapsedInState;

        const float RedDur = 3f;
        const float GreenDur = 5f;
        const float WordDur = 1f;

        public TrafficLightRuntime(int id, Vector2 worldPos, Dir4 lineDir, float phaseOffsetSeconds)
        {
            Id = id;
            WorldPos = worldPos;
            LineDir = lineDir;
            PhaseOffsetSeconds = Mathf.Max(0f, phaseOffsetSeconds);

            // 초기 위상 반영
            ApplyPhaseOffset(PhaseOffsetSeconds);
            UpdateOutputs();
        }

        public void Tick(float dt)
        {
            _elapsedInState += dt;

            if (State == TrafficLightState.Red)
            {
                if (_elapsedInState >= RedDur)
                {
                    State = TrafficLightState.Green;
                    _elapsedInState = 0f;
                }
            }
            else
            {
                if (_elapsedInState >= GreenDur)
                {
                    State = TrafficLightState.Red;
                    _elapsedInState = 0f;
                }
            }

            UpdateOutputs();
        }

        void ApplyPhaseOffset(float seconds)
        {
            // 8초 루프에 맞춰 초기 상태/경과시간 결정
            float t = Mathf.Repeat(seconds, RedDur + GreenDur); // 8s
            if (t < RedDur)
            {
                State = TrafficLightState.Red;
                _elapsedInState = t;
            }
            else
            {
                State = TrafficLightState.Green;
                _elapsedInState = t - RedDur;
            }
        }

        void UpdateOutputs()
        {
            float dur = (State == TrafficLightState.Red) ? RedDur : GreenDur;
            RemainSeconds = Mathf.CeilToInt(Mathf.Max(0f, dur - _elapsedInState));
        }
    }
}
