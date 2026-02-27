using System.Threading;
using AES.Tools;
using AES.Tools.UI.Popups;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using JSAM;
using ParkingCat.Entities.Booster;
using ParkingCat.Game;
using ParkingCat.Game.Args;
using ParkingCat.Game.Booster;
using ParkingCat.Game.BuyHint;
using ParkingCat.Game.Camera;
using ParkingCat.Game.Car.Fx;
using ParkingCat.Game.Facades;
using ParkingCat.Game.Settings;
using ParkingCat.Game.StageFailed;
using ParkingCat.Input;
using ParkingCat.LevelClearRewards;
using ParkingCat.Lobby.Popups.Settings;
using ParkingCat.Move.Ports;
using ParkingCat.Move.UseCases;
using ParkingCat.Persistence;
using ParkingCat.Repository;
using ParkingCat.Sim;
using ParkingCat.Stage.UseCases;
using ParkingCat.Stages;
using ParkingCat.State;
using ParkingCat.UseCase;
using ParkingCat.UseCase.Booster;
using ParkingCat.UseCase.LevelClear;
using ParkingCat.World;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace ParkingCat.InGame
{

    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Scene refs")]
        [SerializeField]
        MonoContext rootContext;
        [SerializeField] BoardCameraController cameraController;
        
        [Header("Simulation Settings")]
        [SerializeField]
        SimulationSettings simSettings = new();
        [SerializeField] int stageNum = 1;
        [SerializeField] LevelClearRewardTableSO rewardTable;

        [Header("Tutorial")]
        [SerializeField] TutorialSequenceSO tutorialSequence;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(simSettings);
        
            builder.RegisterInstance<ILevelClearRewardTable>(rewardTable);
            builder.RegisterInstance(tutorialSequence);
        
            /* =========================
         * Infrastructure
         * ========================= */

            builder.Register<PlayerPrefsTutorialProgress>(Lifetime.Singleton)
                .As<ITutorialProgress>();
            builder.RegisterComponentInHierarchy<TutorialFxController>()
                .AsSelf();

            // Input
            builder.Register<InputGate>(Lifetime.Singleton)
                .As<IInputGate>();

            // StageRun 저장소 (InMemory)
            builder.Register<InMemoryStageRunRepository>(Lifetime.Singleton)
                .As<IStageRunRepository>();

            builder.Register<JsonResourcesStageConfigRepository>(Lifetime.Singleton)
                .As<IStageConfigRepository>();

            // Commit 반영
            builder.Register<StageRunStateWriter>(Lifetime.Singleton)
                .As<IStateWriter>();

            // World / Snapshot
            builder.Register<CarRuntimeRegistry>(Lifetime.Singleton)
                .As<ICarRuntimeRegistry>();

            builder.RegisterComponentInHierarchy<UnityWorldQuery>()
                .As<IWorldQuery>()
                .As<ParkingCat.World.IOneWaySetter>();

            builder.Register<UnityWorldSnapshotProvider>(Lifetime.Singleton)
                .As<IWorldSnapshotProvider>();

            // Simulator
            builder.Register<SimulatorCore>(Lifetime.Singleton);

            builder.Register<SimPreRunner>(Lifetime.Singleton)
                .As<ISimPreRunner>();


            /* =========================
         * Application
         * ========================= */

            // UseCases
            builder.Register<TryMoveUseCase>(Lifetime.Singleton)
                .As<ITryMoveUseCase>();

            builder.Register<StartStageRunUseCase>(Lifetime.Singleton)
                .As<IStartStageRunUseCase>();

            builder.Register<GetStageRunUseCase>(Lifetime.Singleton)
                .As<IGetStageRunUseCase>();

            builder.Register<SaveLevelClearProgressUseCase>(Lifetime.Scoped);
            builder.Register<GrantLevelClearRewardsUseCase>(Lifetime.Scoped);
            builder.Register<HandleLevelClearedUseCase>(Lifetime.Scoped);
            builder.Register<AddMovesWithCoinUseCase>(Lifetime.Scoped);
            builder.Register<AddMovesFromRewardUseCase>(Lifetime.Scoped);
            builder.Register<AddAmbulanceLimitFromRewardUseCase>(Lifetime.Scoped);
            builder.Register<AddAmbulanceLimitWithCoinUseCase>(Lifetime.Scoped);
            
            builder.Register<ClaimStagePendingCoinUseCase>(Lifetime.Scoped);
            builder.Register<TickCashTruckUseCase>(Lifetime.Scoped);
            
            builder.Register<IProbeExitUseCase, ProbeExitUseCase>(Lifetime.Scoped);
            builder.Register<ForceExitUseCase>(Lifetime.Scoped);

            /* =========================
         * Presentation Services
         * ========================= */

            builder.Register<MoveFxPlayer>(Lifetime.Singleton)
                .As<IMoveFxPlayer>();

            // Facade (View 모름)
            builder.Register<GameFacade>(Lifetime.Singleton)
                .As<IGameFacade>();
        
            builder.Register<StageClearPopupVmFactory>(Lifetime.Scoped).As<IStageClearPopupVmFactory>();
            builder.Register<StageClearPopupVM>(Lifetime.Transient);
            
            builder.Register<StageFailedPopupVmFactory>(Lifetime.Scoped).As<IStageFailedPopupVmFactory>();
            builder.Register<StageFailedOutOfMovesPopupVM>(Lifetime.Transient);
            builder.Register<StageFailedAmbulancePopupVM>(Lifetime.Transient);
            
            builder.Register<BuyHintPopupVM>(Lifetime.Transient);
            builder.Register<BuyUFOPopupVM>(Lifetime.Transient);
            
            builder.Register<InGameSettingsPopupVM>(Lifetime.Transient);
            
            builder.RegisterFactory<InGameSettingsPopupVM>(
                resolver => () => resolver.Resolve<InGameSettingsPopupVM>(),
                Lifetime.Scoped);
            
            builder.Register<ExplanationPopupVM>(Lifetime.Transient);
            
            builder.RegisterFactory<ExplanationPopupVM>(
                resolver => () => resolver.Resolve<ExplanationPopupVM>(),
                Lifetime.Scoped);
            
            // Func<T> 명시 등록
            builder.RegisterFactory<StageFailedOutOfMovesPopupVM>(
                resolver => () => resolver.Resolve<StageFailedOutOfMovesPopupVM>(),
                Lifetime.Scoped);
            builder.RegisterFactory<StageFailedAmbulancePopupVM>(
                resolver => () => resolver.Resolve<StageFailedAmbulancePopupVM>(),
                Lifetime.Scoped);
            
            builder.RegisterFactory<BuyHintPopupVM>(
                resolver => () => resolver.Resolve<BuyHintPopupVM>(),
                Lifetime.Scoped);
            builder.RegisterFactory<BuyUFOPopupVM>(
                resolver => () => resolver.Resolve<BuyUFOPopupVM>(),
                Lifetime.Scoped);

            builder.RegisterComponentInHierarchy<PopupHost>();
            builder.Register<PopupService>(Lifetime.Scoped).As<IPopupService>();

            builder.Register<GamePopupFlow>(Lifetime.Scoped).As<IGamePopupFlow>();


            /* =========================
         * ViewModel
         * ========================= */
            
            // BoosterVM
            builder.RegisterFactory<BoosterId, BoosterViewModel>(r => {
                var getBoosterCountUseCase = r.Resolve<GetBoosterCountUseCase>();
                var addBoosterUseCase = r.Resolve<AddBoosterUseCase>();
                var consumeBoosterUseCase = r.Resolve<ConsumeBoosterUseCase>();
                return (id) => new BoosterViewModel(id, getBoosterCountUseCase, addBoosterUseCase, consumeBoosterUseCase);
            }, Lifetime.Scoped);

            builder.Register<BoosterBarViewModel>(Lifetime.Scoped);

            builder.Register<GameViewModel>(Lifetime.Singleton);

            builder.RegisterEntryPoint(r =>
            {
                var vm = r.Resolve<GameViewModel>();

                if (r.TryResolve<ISceneArgsCarrier>(out var carrier) && carrier.TryConsume<StageEnterArgs>(out var stage))
                    stageNum = stage.StageNumber;

                return new GameSceneEntry(stageNum, vm, rootContext, cameraController);
            }, Lifetime.Singleton);
        }

        public class GameSceneEntry : IAsyncStartable
        {
            readonly int stageNum;
            readonly GameViewModel gameViewModel;
            readonly MonoContext rootContext;
            readonly BoardCameraController boardCameraController;


            public GameSceneEntry(int stageNum, GameViewModel gameViewModel, MonoContext rootContext, BoardCameraController boardCameraController)
            {
                this.stageNum = stageNum;
                this.gameViewModel = gameViewModel;
                this.rootContext = rootContext;
                this.boardCameraController = boardCameraController;
            }

            public async UniTask StartAsync(CancellationToken cancellation = default)
            {
                rootContext.SetViewModel(gameViewModel);
                
                if (gameViewModel.HasEditorPreview())
                {
                    gameViewModel.RefreshFromDomain(); // GetStageRun() -> ApplyStageRun
                }
                else
                {
                    await gameViewModel.StartStageCommand.ExecuteAsync(stageNum);
                }

                await UniTask.WaitForEndOfFrame(cancellation);
                boardCameraController.FitToBoard(true);
                
                ADS.ShowBanner();
                //
                // AudioManager.StopAllMusic();
                if (!AudioManager.IsMusicPlaying(NewAudioLibraryMusic.Ingame))
                {
                    AudioManager.PlayMusic(NewAudioLibraryMusic.Ingame, true);    
                }
                
            }

        }

    }
}