using Cysharp.Threading.Tasks;
using ParkingCat.Entities;
using ParkingCat.Repository;


namespace ParkingCat.UseCase
{
    public sealed class AddMovesFromRewardUseCase
    {
        public const int DefaultMoves = 5;

        readonly IStageRunRepository _stageRunRepo;

        public AddMovesFromRewardUseCase(IStageRunRepository stageRunRepo)
        {
            _stageRunRepo = stageRunRepo;
        }

        public UniTask<bool> ExecuteAsync(int addMoves = DefaultMoves)
        {
            if (addMoves <= 0)
                return UniTask.FromResult(false);

            var run = _stageRunRepo.Get();

            if (!run.IsMoveLimited)
                return UniTask.FromResult(false);

            if (run.Status == StageStatus.Cleared)
                return UniTask.FromResult(false);

            run.AddMoves(addMoves);
            run.RecalcOutcome();
            _stageRunRepo.Save(run);

            return UniTask.FromResult(true);
        }
    }
}