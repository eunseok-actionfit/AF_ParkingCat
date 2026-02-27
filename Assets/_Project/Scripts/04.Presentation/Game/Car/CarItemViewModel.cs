using AES.Tools;
using ParkingCat.Entities;
using UnityEngine;


namespace ParkingCat.Game.Car
{
    public sealed class CarItemViewModel
    {
        public int CarId { get; }

        // 위치/회전은 Binder가 Transform에 반영
        public ObservableProperty<Vector2> Pos { get; } = new(Vector2.zero);
        public ObservableProperty<float> HeadingRad { get; } = new();

        // 도메인 runtime state를 단순화해서 View 연출 분기용으로 사용
        public ObservableProperty<bool> IsLeaving { get; } = new();

        public ObservableProperty<int> TurnCursor { get; } = new();
        public ObservableProperty<ValueObjects.TurnProgram> TurnProgram { get; set; } = new();
        
        public ObservableProperty<bool> IsExitHinted { get; } = new();
        
        public ObservableProperty<CarType> Type { get; } = new();
        public ObservableProperty<int> AmbulanceCounter { get; } = new();
        
        public ObservableProperty<int> CashGold { get; } = new(0);
        public ObservableProperty<float> CashCountdown01 { get; } = new(0f);
        
        [Bindable]
        public bool IsCashTruck => Type.Value == CarType.CashTruck;
        
        public Command TryMove { get; set; }
        public GameViewModel Owner { get; set; }

        public CarItemViewModel(GameViewModel parent, int carId)
        {
            CarId = carId;
            Owner = parent;

            TryMove = new Command(execute: () => parent.TryMoveCommand.Execute(carId));
        }
    }
}