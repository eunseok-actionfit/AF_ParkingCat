using ParkingCat.Repository;
using ParkingCat.TurnProgram.DTOs;
using ParkingCat.ValueObjects;


namespace ParkingCat.TurnProgram.UseCases
{
    public sealed class SetTurnProgramUseCase : ISetTurnProgramUseCase
    {
        readonly IStageRunRepository _runRepo;

        public SetTurnProgramUseCase(IStageRunRepository runRepo)
        {
            _runRepo = runRepo;
        }

        public SetTurnProgramResponse Execute(SetTurnProgramRequest request)
        {
            var run = _runRepo.Get();
            var cars = run.Stage.Cars;

            for (int i = 0; i < cars.Count; i++)
            {
                var car = cars[i];
                if (car.Id != request.CarId) continue;

                car.TurnProgram.Actions = request.Actions ?? System.Array.Empty<TurnAction>();
                car.TurnProgram.Loop = request.Loop;

                if (request.ResetCursor)
                    car.TurnCursor = 0;

                _runRepo.Save(run);

                return new SetTurnProgramResponse
                {
                    Applied = true,
                    Reason = "OK",
                };
            }

            return new SetTurnProgramResponse
            {
                Applied = false,
                Reason = "CarNotFound",
            };
        }
    }
}