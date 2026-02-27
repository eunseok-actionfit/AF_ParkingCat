using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.Move.UseCases;
using ParkingCat.Sim;
using ParkingCat.Stage.DTOs;
using ParkingCat.Stage.UseCases;
using ParkingCat.UseCase.Booster;


namespace ParkingCat.Game.Facades
{
    public sealed class GameFacade : IGameFacade
    {
        readonly IStartStageRunUseCase _start;
        readonly IGetStageRunUseCase _get;
        readonly ITryMoveUseCase _tryMove;
        readonly IProbeExitUseCase _probe;
        readonly ConsumeBoosterUseCase _consumeBoosterUseCase;

        StageRunDto _preview;

        public bool HasPreviewStageRun => _preview != null;


        public GameFacade(IStartStageRunUseCase start, IGetStageRunUseCase get, ITryMoveUseCase tryMove, IProbeExitUseCase probe, ConsumeBoosterUseCase consumeBoosterUseCase)
        {
            _start = start;
            _get = get;
            _tryMove = tryMove;
            _probe = probe;
            _consumeBoosterUseCase = consumeBoosterUseCase;
        }

        public UniTask<StageRunDto> StartStageAsync(int stageId) => _start.ExecuteAsync(stageId);

        public StageRunDto GetStageRun()
            => _preview ?? _get.Execute();

        public SimResult TryMove(int moverCarId) => _tryMove.Execute(moverCarId);

        public void SetPreviewStageRun(StageRunDto dto) => _preview = dto;
        public void ClearPreviewStageRun() => _preview = null;
        public bool WouldExit(int carId) => _probe.WouldExit(carId);

        public async UniTask<bool> TryConsumeBooster(BoosterId id)
        {
            var ok = await _consumeBoosterUseCase.ExecuteAsync(id);
            return ok;
        }

    }
}