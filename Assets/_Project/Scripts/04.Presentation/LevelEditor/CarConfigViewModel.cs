using System;
using AES.Tools;
using ParkingCat.Config;
using ParkingCat.Entities;
using ParkingCat.Game.Car.Binding;
using ParkingCat.LevelEditor.Util;
using UnityEngine;


namespace ParkingCat.LevelEditor
{

    public sealed class CarConfigViewModel
    {
        public ObservableProperty<int> CarId { get; } = new(1);
        public ObservableProperty<Vector2Int> HeadCell { get; } = new(Vector2Int.zero);
        public ObservableProperty<int> Length { get; } = new(2);
        public ObservableProperty<Dir4> Dir { get; } = new(Dir4.Right);
        
        public ObservableProperty<CarType> Type { get; } = new();
        public ObservableProperty<int> AmbulanceEscapeMoveLimit { get; } = new(5);
        public ObservableProperty<int> CashGold { get; } = new(100);
        
        public  ObservableProperty<bool> IsCashTruck { get; } = new();
        public ObservableProperty<bool> IsAmbulance { get; } = new();
        
        public ObservableProperty<TurnShape> TurnMode { get; } = new();

        public Command<int> SetDirCommand;
        public Command<int> SetTurnModeCommand;

        public CarConfigViewModel(Action setDirCallback = null)
        {
            SetDirCommand = new Command<int>(i =>
            {
                Dir.Value = (Dir4)i;
                setDirCallback?.Invoke();
            },
            canExecute: i =>  Dir.Value != (Dir4)i );
            SetTurnModeCommand = new Command<int>(i =>
            {
                TurnMode.Value = (TurnShape)i;
                setDirCallback?.Invoke();
            },
                canExecute: i => TurnMode.Value != (TurnShape)i);
            AmbulanceEscapeMoveLimit.OnValueChanged +=  _ => setDirCallback?.Invoke();
            CashGold.OnValueChanged +=  _ => setDirCallback?.Invoke();
            Type.OnValueChanged += _ =>
            {
                IsAmbulance.Value = Type.Value == CarType.Ambulance;
                IsCashTruck.Value = Type.Value == CarType.CashTruck;
            };
        }

        public StageConfig.CarConfig ToConfig()
        {
            return new StageConfig.CarConfig
            {
                CarId = CarId.Value,
                HeadCell = HeadCell.Value,
                Length = Math.Max(1, Length.Value),
                Dir = Dir.Value,
                TurnProgram = CarTurnMapper.ToActions(TurnMode.Value),

                Type = Type.Value,
                EscapeMoveLimit = AmbulanceEscapeMoveLimit.Value,
                InitialCashGold = CashGold.Value,
            };
        }

        public void FromConfig(StageConfig.CarConfig cfg)
        {
            CarId.Value = cfg.CarId;
            HeadCell.Value = cfg.HeadCell;
            Length.Value = cfg.Length;
            Dir.Value = cfg.Dir;
            TurnMode.Value = CarTurnMapper.FromActions(cfg.TurnProgram);

            Type.Value = cfg.Type;
            AmbulanceEscapeMoveLimit.Value = cfg.EscapeMoveLimit;
            CashGold.Value = cfg.InitialCashGold;
            IsAmbulance.Value = cfg.Type == CarType.Ambulance;
            IsCashTruck.Value = cfg.Type == CarType.CashTruck;
        }

    }
}