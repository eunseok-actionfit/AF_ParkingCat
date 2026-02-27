using AES.Tools;
using ParkingCat.Entities;
using UnityEngine;

namespace ParkingCat.Game.TrafficLight
{


    /// <summary>
    /// 런타임 신호등 상태(ViewModel).
    /// - GameViewModel이 유지/갱신 (ApplyStageRun 또는 별도 갱신 루프)
    /// - View는 Binder를 통해 구독
    /// </summary>
    public sealed class TrafficLightItemViewModel
    {
        public ObservableProperty<int> Id { get; } = new();

        // 월드 위치(신호등 아이콘 위치)
        public ObservableProperty<Vector2> WorldPos { get; } = new();

        // 판정선(에디터/연출용 표시를 위해 남겨둠)
        public ObservableProperty<Dir4> LineDir { get; } = new(Dir4.Right);
        public ObservableProperty<int> LineLength { get; } = new(1);

        // 상태/표시
        public ObservableProperty<TrafficLightState> State { get; } = new();
        public ObservableProperty<int> RemainSeconds { get; } = new();
        public ObservableProperty<string> DisplayText { get; } = new();



        float _elapsedInState;

        const float RedDur = 3f;
        const float GreenDur = 5f;
        
        public void InitPhase(float phaseOffsetSeconds)
        {
            float t = Mathf.Repeat(phaseOffsetSeconds, RedDur + GreenDur);

            if (t < RedDur)
            {
                State.Value = TrafficLightState.Red;
                _elapsedInState = t;
            }
            else
            {
                State.Value = TrafficLightState.Green;
                _elapsedInState = t - RedDur;
            }

            UpdateOutputs();
        }

        public void Tick(float dt)
        {
            _elapsedInState += dt;

            if (State.Value == TrafficLightState.Red)
            {
                if (_elapsedInState >= RedDur)
                {
                    State.Value = TrafficLightState.Green;
                    _elapsedInState = 0f;
                }
            }
            else
            {
                if (_elapsedInState >= GreenDur)
                {
                    State.Value = TrafficLightState.Red;
                    _elapsedInState = 0f;
                }
            }

            UpdateOutputs();
        }

        void UpdateOutputs()
        {
            float dur = (State.Value == TrafficLightState.Red) ? RedDur : GreenDur;
            int remain = Mathf.CeilToInt(Mathf.Max(0f, dur - _elapsedInState));

            RemainSeconds.Value = remain;

            // 표시 규칙:
            // Red: STOP,2,1  (3은 숨김)
            // Green: GO,4,3,2,1 (5는 숨김)
            if (State.Value == TrafficLightState.Red)
            {
                if (remain >= RedDur) DisplayText.Value = "STOP";
                else DisplayText.Value = remain.ToString(); // 2,1
            }
            else
            {
                if (remain >= GreenDur) DisplayText.Value = "GO";
                else DisplayText.Value = remain.ToString(); // 4,3,2,1
            }
        }
    }
}