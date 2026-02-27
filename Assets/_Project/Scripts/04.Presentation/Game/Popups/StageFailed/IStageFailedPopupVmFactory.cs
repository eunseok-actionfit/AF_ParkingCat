using System;
using AES.Tools.UI.Popups;
using ParkingCat.Game;
using ParkingCat.Game.StageFailed;


public interface IStageFailedPopupVmFactory
{
    PopupViewModelBase Create(StageFailReason reason);
}

public sealed class StageFailedPopupVmFactory : IStageFailedPopupVmFactory
{
    readonly Func<StageFailedOutOfMovesPopupVM> _outOfMoves;
    readonly Func<StageFailedAmbulancePopupVM> _ambulance;

    public StageFailedPopupVmFactory(
        Func<StageFailedOutOfMovesPopupVM> outOfMoves,
        Func<StageFailedAmbulancePopupVM> ambulance)
    {
        _outOfMoves = outOfMoves;
        _ambulance = ambulance;
    }

    public PopupViewModelBase Create(StageFailReason reason)
        => reason switch
        {
            StageFailReason.OutOfMoves => _outOfMoves(),
            StageFailReason.AmbulanceGimmick => _ambulance(),
            _ => _outOfMoves(),
        };
}
