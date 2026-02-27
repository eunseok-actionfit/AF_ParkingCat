using System;
using AES.Tools;
using AES.Tools.UI.Popups;
using ParkingCat.Game;
using ParkingCat.Game.StageFailed;
using UnityEngine;


public enum ExplanationType
{
    Hint,
    Ufo,
    TrafficLight,
    Ambulance,
    CashTruck,
    OneWay,
    TwoLane,
}

public sealed class ExplanationPopupVM : PopupViewModelBase
{
    public ObservableProperty<ExplanationType> Type { get; } = new();
    public Command CloseCommand { get; }
    
    public ExplanationPopupVM()
    {
        CloseCommand = new Command(Close);
    }
    
    public void SetType(ExplanationType type) => this.Type.Value = type;
    
}
