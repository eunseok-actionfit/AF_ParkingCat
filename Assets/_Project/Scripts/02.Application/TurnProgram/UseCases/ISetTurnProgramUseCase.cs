using ParkingCat.TurnProgram.DTOs;


namespace ParkingCat.TurnProgram.UseCases
{
    public interface ISetTurnProgramUseCase
    {
        SetTurnProgramResponse Execute(SetTurnProgramRequest request);
    }
}