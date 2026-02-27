using System;
using AES.Tools;
using ParkingCat.Config;
using UnityEngine;

namespace ParkingCat.LevelEditor
{
    [Serializable]
    public sealed class OneWayLaneConfigViewModel
    {
        public ObservableProperty<Vector2Int> StartCell { get; } = new();
        public ObservableProperty<Dir4> Dir { get; } = new(Dir4.Right);
        public ObservableProperty<int> Length { get; } = new(1);
        public ObservableProperty<int> Thickness { get; } = new(1);
        public ObservableProperty<int> LaneIndex { get; } = new(0);
        public ObservableProperty<Dir4> AllowedDir { get; } = new(Dir4.Right);

        public StageConfig.OneWayLaneConfig ToConfig()
        {
            return new StageConfig.OneWayLaneConfig
            {
                StartCell = StartCell.Value,
                Dir = Dir.Value,
                Length = Length.Value,
                Thickness = Thickness.Value,
                LaneIndex = LaneIndex.Value,
                AllowedDir = AllowedDir.Value
            };
        }

        public void FromConfig(StageConfig.OneWayLaneConfig cfg)
        {
            if (cfg == null) return;
            StartCell.Value = cfg.StartCell;
            Dir.Value = cfg.Dir;
            Length.Value = cfg.Length;
            Thickness.Value = cfg.Thickness;
            LaneIndex.Value = cfg.LaneIndex;
            AllowedDir.Value = cfg.AllowedDir;
        }
    }
}