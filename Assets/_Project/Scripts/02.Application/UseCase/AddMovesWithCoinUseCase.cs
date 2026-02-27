using Cysharp.Threading.Tasks;
using ParkingCat.Entities;
using ParkingCat.Repository;
using ParkingCat.UseCase.Economy.Coin;


namespace ParkingCat.UseCase
{
    public sealed class AddMovesWithCoinUseCase
    {
        public const int DefaultCost  = 900;
        public const int DefaultMoves = 5;

        readonly IStageRunRepository _stageRunRepo;
        readonly SpendCoinUseCase _spendCoinUseCase;

        public AddMovesWithCoinUseCase(
            IStageRunRepository stageRunRepo,
            SpendCoinUseCase spendCoinUseCase)
        {
            _stageRunRepo = stageRunRepo;
            _spendCoinUseCase = spendCoinUseCase;
        }

        public async UniTask<bool> ExecuteAsync(
            int cost = DefaultCost,
            int addMoves = DefaultMoves)
        {
            if (addMoves <= 0) return false;

            var run = _stageRunRepo.Get();

            // 이동 제한 스테이지가 아니면 의미 없음
            if (!run.IsMoveLimited)
                return false;

            // 이미 클리어면 불가
            if (run.Status == StageStatus.Cleared)
                return false;

            // 1) 코인 차감
            var paid = await _spendCoinUseCase.ExecuteAsync(cost);
            if (!paid)
                return false;

            // 2) 이동 추가
            run.AddMoves(addMoves);

            // 3) 실패 상태였다면 Playing으로 복귀
            run.RecalcOutcome();

            // 4) 저장
            _stageRunRepo.Save(run);

            return true;
        }
    }
}