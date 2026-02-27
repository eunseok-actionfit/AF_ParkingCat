using AES.Tools;
using ParkingCat.Config;
using UnityEngine;

namespace ParkingCat.LevelEditor
{
    public sealed class TrafficLightConfigViewModel
    {
        public ObservableProperty<int> Id { get; } = new(1);

        public ObservableProperty<Vector2> WorldPos { get; } = new(Vector2.zero);

        public ObservableProperty<Dir4> LineDir { get; } = new(Dir4.Right);
        public ObservableProperty<int> LineLength { get; } = new(1);

        public ObservableProperty<float> PhaseOffsetSeconds { get; } = new(0f);

        public StageConfig.TrafficLightConfig ToConfig()
        {
            return new StageConfig.TrafficLightConfig
            {
                Id = Id.Value,
                WorldPos = WorldPos.Value,
                LineDir = LineDir.Value,
                PhaseOffsetSeconds = Mathf.Max(0f, PhaseOffsetSeconds.Value),
            };
        }

        public void FromConfig(StageConfig.TrafficLightConfig cfg)
        {
            Id.Value = cfg.Id;
            WorldPos.Value = cfg.WorldPos;
            LineDir.Value = cfg.LineDir;
            PhaseOffsetSeconds.Value = Mathf.Max(0f, cfg.PhaseOffsetSeconds);
        }
    }
}