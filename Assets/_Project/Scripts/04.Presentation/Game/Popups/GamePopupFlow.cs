using System;
using AES.Tools.UI.Popups;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities;
using ParkingCat.Game.BuyHint;
using ParkingCat.Game.Settings;
using ParkingCat.Game.StageFailed;
using ParkingCat.Lobby.Popups.Settings;
using ParkingCat.Repository;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.Game
{
    public sealed class GamePopupFlow : IGamePopupFlow
    {
        readonly IPopupService _popups;
        readonly IStageClearPopupVmFactory _factory;
        readonly IStageFailedPopupVmFactory _failedFactory;
        readonly Func<BuyHintPopupVM> _buyHintVM;
        readonly Func<BuyUFOPopupVM> _buyUfoVM;
        readonly IStageRunRepository _stageRunRepo;
        readonly Func<InGameSettingsPopupVM> _settingsVm;
        readonly Func<ExplanationPopupVM> _explanationVM;

        public GamePopupFlow(
            IPopupService popups,
            IStageClearPopupVmFactory factory,
            Func<BuyHintPopupVM> buyHintVM,
            Func<BuyUFOPopupVM> buyUfoVM,
            IStageFailedPopupVmFactory failedFactory,
            IStageRunRepository stageRunRepo,
            Func<InGameSettingsPopupVM> settingsVm,
            Func<ExplanationPopupVM> explanationVM)
        {
            _popups = popups;
            _factory = factory;
            _buyHintVM = buyHintVM;
            _buyUfoVM = buyUfoVM;
            _failedFactory = failedFactory;
            _stageRunRepo = stageRunRepo;
            _settingsVm = settingsVm;
            _explanationVM = explanationVM;
        }

        public async UniTask ShowStageClearAsync(int levelId, LevelDifficulty difficulty)
        {
            var vm = _factory.Create(levelId, difficulty);
            if (_popups.TryShow<StageClearPopupView>(vm, out var task, modal: true))
                await task;
        }

        public async UniTask ShowStageFailedAsync(StageFailReason reason)
        {
            while (true)
            {
                var vm = _failedFactory.Create(reason);

                if (!_popups.TryShow<StageFailedPopupView>(vm, out var task, modal: true))
                    return;

                // 여기 await가 "완전히 닫힘(애니/스택 제거)"까지 보장해줘서
                // 다음 TryShow가 false로 떨어지는 문제를 구조적으로 제거함.
                await task;

                var run = _stageRunRepo.Get();
                if (run == null) return;

                if (run.Status != StageStatus.Failed)
                    return;

                reason = ResolveFailReason(run);
            }
        }

        static StageFailReason ResolveFailReason(StageRun run)
        {
            // 우선순위: 앰뷸 > 이동횟수
            var cars = run.Stage?.Cars;
            if (cars != null)
            {
                for (int i = 0; i < cars.Count; i++)
                {
                    var c = cars[i];
                    if (c == null || c.IsLeaving) continue;
                    if (!c.IsAmbulance) continue;

                    if (c.RemainingEscapeMoves <= 0)
                        return StageFailReason.AmbulanceGimmick;
                }
            }
            return StageFailReason.OutOfMoves;
        }

        public async UniTask ShowBuyHintAsync()
        {
            var vm = _buyHintVM();
            if (_popups.TryShow<BuyHintPopupView>(vm, out var task, modal: true))
                await task;
        }

        public UniTask ShowBuyUFOAsync()
        {
            var vm = _buyUfoVM();
            _popups.TryShow<BuyUFOPopupView>(vm, out var task, modal: true);
            return task;
        }
        
        
        public async UniTask ShowSettingsAsync(int levelId)
        {
            var vm = _settingsVm();
            vm.SetStageId(levelId);
            if (_popups.TryShow<InGameSettingsPopupView>(vm, out var task, modal: true))
                await task;
        }

        public async UniTask ShowExplanationAsync(ExplanationType type)
        {
            var vm = _explanationVM();
            vm.SetType(type);
            if (_popups.TryShow<ExplanationPopupView>(vm, out var task, modal: true))
                await task;
        }

    }
}
 