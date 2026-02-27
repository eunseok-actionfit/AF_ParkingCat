using System;
using System.Collections.Generic;
using System.Threading;
using AES.Tools;
using Cysharp.Threading.Tasks;
using JSAM;
using ParkingCat.Economy;
using ParkingCat.Entities;
using ParkingCat.Entities.Booster;
using ParkingCat.Game.Booster;
using ParkingCat.Game.Car;
using ParkingCat.Game.Car.Fx;
using ParkingCat.Game.Facades;
using ParkingCat.Game.TrafficLight;
using ParkingCat.Move.Ports;
using ParkingCat.Move.UseCases;
using ParkingCat.Sim;
using ParkingCat.Stage.DTOs;
using ParkingCat.ValueObjects.Level;
using ParkingCat.World;
using UnityEngine;


namespace ParkingCat.Game
{
    public enum StageFailReason
    {
        OutOfMoves,
        AmbulanceGimmick,
    }
    public sealed class GameViewModel : IDisposable
    {
        public enum BoosterMode { None, ForceExit }

        readonly IGameFacade _facade;
        readonly IMoveFxPlayer _fx;
        readonly ForceExitUseCase _forceExit;

        // ForceExit 비주얼을 위해 런타임 레지스트리 접근 필요
        readonly ICarRuntimeRegistry _runtime;

        public ObservableProperty<int> CurrentLevel { get; } = new();
        public ObservableProperty<LevelDifficulty> CurrentDifficulty { get; } = new();
        public ObservableProperty<int> RemainingMoves { get; } = new(-1);
        public ObservableProperty<StageStatus> Status { get; } = new();

        public ObservableList<CarItemViewModel> Cars { get; } = new();
        public ObservableProperty<MoveEvent> LastMoveEvent { get; } = new();

        public ObservableList<TrafficLightItemViewModel> TrafficLights { get; } = new();

        readonly Dictionary<int, TrafficLightItemViewModel> _trafficLightMap = new();

        public ObservableProperty<int[,]> Grid01 { get; } = new();

        public ObservableProperty<int> PendingCoin { get; } = new(0);

        
        // ---- Commands (기존) ----
        public AsyncCommand<int> StartStageCommand { get; }
        public AsyncCommand BackLobbyCommand { get; }
        public AsyncCommand<int> TryMoveCommand { get; }
        public Command RefreshCommand { get; }

        // Popups
        public AsyncCommand OpenStageClearCommand { get; }
        public AsyncCommand<StageFailReason> OpenStageFailedCommand { get; }
        public AsyncCommand OpenBuyHintCommand { get; }
        public AsyncCommand OpenBuyUFOCommand { get; }
        public AsyncCommand OpenSettingCommand { get; }
        public AsyncCommand<ExplanationType> OpenExplanationCommand { get; }

        [Bindable]
        public bool IsUnlimitMoves => RemainingMoves.Value == -1;

        [Bindable]
        public CoinViewModel CoinVM { get; private set; }

        [Bindable]
        public BoosterBarViewModel BoosterBarViewModel { get; }
        [Bindable]
        public bool EnableHint => CurrentLevel.Value >= 10;
        [Bindable]
        public bool EnableUfo => CurrentLevel.Value >= 12;
        
        public Entities.Stage Stage { get; private set; }


        // ---- Booster / Tap routing ----
        public ObservableProperty<BoosterMode> CurrentBoosterMode { get; } = new();

        // 차량 탭 분기
        public AsyncCommand<int> OnCarTappedCommand { get; }

        // ForceExit 모드 진입/취소
        public Command EnterForceExitModeCommand { get; }
        public Command ExitForceExitModeCommand { get; }

        // Hint 부스터: 랜덤 1개 켜기(여러번 가능)
        public Command UseExitHintBoosterCommand { get; }

        // (디버그/수동) 전체 힌트 끄기
        public Command ClearAllExitHintsCommand { get; }

        // ---- internal ----
        int _moveSeq = 0;
        readonly Dictionary<int, CarItemViewModel> _carMap = new();

        // 여러 개 힌트를 유지해야 함
        readonly HashSet<int> _hintedCarIds = new();

        CancellationTokenSource _trafficLightCts;
        const float TrafficLightTickDt = 1f;

        StageStatus? _pendingResultStatus = null;
        
        public ObservableProperty<StageRunDto.OneWayLaneDto[]> OneWayLanes { get; } = new();

        readonly IOneWaySetter _oneWaySetter;
        EventBinding<TrafficLightPenaltyEvent> _tlPenaltyBinding = new();
        
        public bool IsTutorialActive { get; private set; }
        public int TutorialAllowedCarId { get; private set; } = -1;

        TutorialSequenceSO _tutorialSo;
        ITutorialProgress _tutorialProgress;
        TutorialFxController _tutorialFx;
        IStateWriter _stateWriter;

        int _tutorialStepIndex = -1;
        
        public GameViewModel(
            IGameFacade facade,
            IMoveFxPlayer fx,
            ICarRuntimeRegistry runtime,
            IInputGate inputGate,
            IGamePopupFlow gamePopup,
            SceneFlow.SceneFlow sceneFlow,
            CoinViewModel coinVM,
            ForceExitUseCase forceExit,
            BoosterBarViewModel boosterBarViewModel,
            IOneWaySetter oneWaySetter,
            TickCashTruckUseCase tickCashTruck,
            TutorialSequenceSO tutorialSo,
            ITutorialProgress tutorialProgress,
            TutorialFxController tutorialFx,
            IStateWriter stateWriter)
        {
            _facade = facade;
            _fx = fx;
            _runtime = runtime;
            CoinVM = coinVM;
            _forceExit = forceExit;
            BoosterBarViewModel = boosterBarViewModel;
            _oneWaySetter = oneWaySetter;
            _tickCashTruck = tickCashTruck;
            _tutorialSo = tutorialSo;
            _tutorialProgress = tutorialProgress;
            _tutorialFx = tutorialFx;
            _stateWriter = stateWriter;

            StartStageCommand = new AsyncCommand<int>(async stage =>
            {
                CurrentLevel.Value = stage;
                await StartStageAsync(stage);
                
                ShowExplanationIfNeeded();
            });

            RefreshCommand = new Command(RefreshFromDomain);

            BackLobbyCommand = new AsyncCommand(_ => sceneFlow.GoLobbyAsync());

            // 기존 TryMoveCommand 유지
            TryMoveCommand = new AsyncCommand<int>(
                async id =>
                {
                    await TryMoveAsync(id);
                },
                _ => !inputGate.Locked && (RemainingMoves.Value > 0 || RemainingMoves.Value == -1)
            );

            OpenStageClearCommand = new AsyncCommand(_ =>
                gamePopup.ShowStageClearAsync(CurrentLevel.Value, CurrentDifficulty.Value));

            OpenStageFailedCommand = new AsyncCommand<StageFailReason>(async reason =>
            {
                Debug.Log($"Stage Failed: {reason}");
                await gamePopup.ShowStageFailedAsync(reason);
                RefreshCommand.Execute();
            });

            OpenBuyHintCommand = new AsyncCommand(_ => gamePopup.ShowBuyHintAsync());
            OpenBuyUFOCommand = new AsyncCommand(_ => gamePopup.ShowBuyUFOAsync());
            OpenSettingCommand = new AsyncCommand(_ => gamePopup.ShowSettingsAsync(CurrentLevel.Value));
            OpenExplanationCommand = new AsyncCommand<ExplanationType>(async type => 
               await gamePopup.ShowExplanationAsync(type));

            // ---- Booster Commands ----
            EnterForceExitModeCommand = new Command(() =>
            {
                if (boosterBarViewModel.Get(BoosterId.ForceExit).Count.Value <= 0)
                    OpenBuyUFOCommand.ExecuteAsync().Forget();
                else { CurrentBoosterMode.Value = BoosterMode.ForceExit; }
            });

            ExitForceExitModeCommand = new Command(() =>
            {
                CurrentBoosterMode.Value = BoosterMode.None;
            });

            UseExitHintBoosterCommand = new Command(() =>
            {
                TurnOnRandomExitHintOne().Forget();
            });

            ClearAllExitHintsCommand = new Command(ClearAllExitHints);

            // ---- Tap routing ----
            OnCarTappedCommand = new AsyncCommand<int>(
                async carId =>
                {
                    if (CurrentBoosterMode.Value == BoosterMode.ForceExit)
                    {

                        CurrentBoosterMode.Value = BoosterMode.None;
                        await ForceExitAsync(carId);
                        await BoosterBarViewModel.ForceExit.RefreshAsync();
                        return;
                    }

                    await TryMoveAsync(carId);
                },
                carId =>
                {
                    if (inputGate.Locked
                        || !_carMap.TryGetValue(carId, out var vm)
                        || vm.IsLeaving.Value 
                        || Status.Value != StageStatus.Playing)
                    {
                        if (CurrentBoosterMode.Value == BoosterMode.ForceExit)
                            CurrentBoosterMode.Value = BoosterMode.None;

                        return false;
                    }
                    
                    return  RemainingMoves.Value > 0 || RemainingMoves.Value == -1;;
                }
            );

            Status.Subscribe(OnStatusChanged);
            
            if (_fx != null)
            {
                
                _fx.OnAllFxCompleted += HandleAllFxCompleted;
            }
            
            _tlPenaltyBinding?.Deregister();
            _tlPenaltyBinding = new EventBinding<TrafficLightPenaltyEvent>()
            {
                Name = "TrafficLightPenalty->GameViewModel",
                Owner = this
            }.Register(ApplyTrafficLightPenaltyOncePerMove);
            
            LastMoveEvent.Subscribe(e =>
            {
                if (!IsTutorialActive) return;
                if (e.CarId != TutorialAllowedCarId) return;

                AdvanceTutorial(); // 다음 스텝으로
            });
        }

        void ShowExplanationIfNeeded()
        {
            var stageId = CurrentLevel.Value;

            switch (stageId)
            {
                case 7:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.TrafficLight).Forget();
                    break;
                case 9:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.TwoLane).Forget();
                    break;
                case 10:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.Hint).Forget();
                    break;
                case 12:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.Ufo).Forget();
                    break;
                case 16:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.OneWay).Forget();
                    break;
                case 20:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.Ambulance).Forget();
                    break;
                case 30:
                    OpenExplanationCommand.ExecuteAsync(ExplanationType.CashTruck).Forget();
                    break;
            }
        }

        void OnStatusChanged(StageStatus status)
        {
            switch (status)
            {
                case StageStatus.Playing:
                    break;

                case StageStatus.Cleared:
                case StageStatus.Failed:
                    // FX 재생 중이면 보류
                    if (_fx is { IsPlaying: true })
                    {
                        _pendingResultStatus = status;
                        return;
                    }

                    // FX 없거나 이미 끝났으면 즉시 표시
                    ShowResultPopup(status);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        void HandleAllFxCompleted()
        {
            if (_pendingResultStatus == null) return;
            
             RefreshFromDomain();

            var status = Status.Value;

            if (status == StageStatus.Cleared || status == StageStatus.Failed)
                ShowResultPopup(status);

            _pendingResultStatus = null;
        }

        void ShowResultPopup(StageStatus status)
        {
            if (status == StageStatus.Cleared)
            {
                Haptic.Weak();
                AudioManager.PlaySound(NewAudioLibrarySounds.Success);
                OpenStageClearCommand.Execute();
                return;
            }

            if (status == StageStatus.Failed)
            {
                Haptic.Weak();
                AudioManager.PlaySound(NewAudioLibrarySounds.LevelFail);
                var reason = ResolveFailReason();
                OpenStageFailedCommand.Execute(reason);
            }
        }
        
        StageFailReason ResolveFailReason()
        {
            // 1순위: 앰뷸런스 실패
            for (int i = 0; i < Cars.Count; i++)
            {
                var car = Cars[i];

                if (car.Type.Value == CarType.Ambulance &&
                    car.AmbulanceCounter.Value <= 0)
                {
                    return StageFailReason.AmbulanceGimmick;
                }
            }

            // 기본: 이동횟수 소진
            return StageFailReason.OutOfMoves;
        }

        
        public async UniTask StartStageAsync(int stageId)
        {
            StageRunDto dto = await _facade.StartStageAsync(stageId);
            ApplyStageRun(dto);

            CurrentBoosterMode.Value = BoosterMode.None;

            // 새 스테이지 시작하면 힌트는 리셋이 자연스러움
            ClearAllExitHints();
            
            TryAutoStartTutorial(stageId);
            
            StartTrafficLightLoop();
            StartCashTruckLoop();
        }

        void TryAutoStartTutorial(int stageId)
        {
            if (_tutorialSo == null) return;               
            if (_tutorialSo.stageId != stageId) return;
            if (_tutorialProgress != null && _tutorialProgress.IsCompleted(stageId)) return;

            BeginTutorial();
        }
        
        void AdvanceTutorial()
        {
            _tutorialStepIndex++;

            // 마지막 스텝이면 종료
            if (_tutorialStepIndex >= _tutorialSo.steps.Count)
            {
                EndTutorial(markCompleted: true);
                return;
            }

            ApplyTutorialStep();
        }


        void BeginTutorial()
        {
            IsTutorialActive = true;
            _tutorialStepIndex = 0;
            ApplyTutorialStep();
        }
        
        void ApplyTutorialStep()
        {
            _tutorialFx.Clear(); 
            
            var step = _tutorialSo.steps[_tutorialStepIndex];
            int carId = TutorialTargetResolver.ResolveCarId(this, step.target);

            TutorialAllowedCarId = carId;


            // if (_carMap.TryGetValue(carId, out var vm))
            //     vm.IsExitHinted.Value = true;
            //
            // // 화살표 FX: carId → transform 바인드
             _tutorialFx.BindToCar(carId);
        }


        void EndTutorial(bool markCompleted)
        {
            IsTutorialActive = false;
            TutorialAllowedCarId = -1;
            _tutorialStepIndex = -1;

            _tutorialFx.Clear(); 

            // if (markCompleted && _tutorialProgress != null)
            //     _tutorialProgress.MarkCompleted(_tutorialSo.stageId);
        }
        
        public void RefreshFromDomain()
        {
            var dto = _facade.GetStageRun();
            ApplyStageRun(dto);
        }

        UniTask TryMoveAsync(int carId)
        {
            ClearExitHintForCar(carId);
            
            Haptic.Weak();

            SimResult result = _facade.TryMove(carId);

            if (RemainingMoves.Value > 0)
                RemainingMoves.Value -= 1; // 기본 -1 

            _moveSeq++;
            LastMoveEvent.Value = new MoveEvent(_moveSeq, carId, result);
          
            if (_fx != null)
                _fx.PlayAsync(carId, result).Forget(); // HitFx + 롤백은 여기서 재생
            RefreshFromDomain();
            return UniTask.CompletedTask;
        }

        async UniTask ForceExitAsync(int carId)
        {
            var ok = await _facade.TryConsumeBooster(BoosterId.ForceExit);

            if (!ok) { return; }

            if (_runtime != null && _runtime.TryGet(carId, out var info) && info.PosTf != null)
            {
                var view = info.PosTf.GetComponent<CarView>();

                if (view != null)
                {
                    view.MarkLeaving();

                    await view.PlayForceExitAsync();
                    UnityEngine.Object.Destroy(view.gameObject);
                }

                // 레지스트리 정리(Commit 때도 Unregister 함) 
                _runtime.Unregister(carId);
            }

            // 도메인 상태 반영 (Leaving + outcome)
            _forceExit.Execute(carId);

            // 해당 차가 힌트 대상이면 힌트에서도 제거
            _hintedCarIds.Remove(carId);

            RefreshFromDomain();
        }

        void ApplyStageRun(StageRunDto dto)
        {
            RemainingMoves.Value = dto.RemainingMoves;
            Status.Value = dto.Status;
            CurrentDifficulty.Value = dto.Difficulty;
            Grid01.Value = dto.Grid01;
            OneWayLanes.Value = dto.OneWayLanes;
            _oneWaySetter?.SetOneWay(dto.OneWayLanes);
            PendingCoin.Value = dto.PendingCoin;
            
            var desired = new List<CarItemViewModel>(dto.Cars.Length);
            var alive = new HashSet<int>();

            for (int i = 0; i < dto.Cars.Length; i++)
            {
                var c = dto.Cars[i];
                alive.Add(c.Id);

                if (!_carMap.TryGetValue(c.Id, out var vm))
                {
                    vm = new CarItemViewModel(this, c.Id);
                    _carMap[c.Id] = vm;
                }

                vm.Pos.Value = new Vector2(c.Pos.x, c.Pos.y);
                vm.HeadingRad.Value = c.HeadingRad;
                vm.IsLeaving.Value = (c.RuntimeState.ToString() == "Leaving");
                vm.TurnCursor.Value = c.TurnCursor;
                vm.TurnProgram.Value = new ValueObjects.TurnProgram() { Actions = c.TurnActions };
                vm.Type.Value = c.Type;
                vm.AmbulanceCounter.Value = c.RemainingEscapeMoves;
                vm.CashGold.Value = c.CashGold;
                vm.CashCountdown01.Value = c.CashCountdown01;

                desired.Add(vm);
            }

            // 리스트에서 사라진 항목 제거
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                var vm = Cars[i];

                if (!alive.Contains(vm.CarId))
                {
                    _hintedCarIds.Remove(vm.CarId);
                    Cars.RemoveAt(i);
                    _carMap.Remove(vm.CarId);
                }
            }

            // 순서 최소 변경
            for (int i = 0; i < desired.Count; i++)
            {
                if (i < Cars.Count && ReferenceEquals(Cars[i], desired[i])) continue;

                int found = Cars.IndexOf(desired[i]);

                if (found >= 0)
                {
                    Cars.RemoveAt(found);
                    Cars.Insert(i, desired[i]);
                }
                else { Cars.Insert(i, desired[i]); }
            }

            while (Cars.Count > desired.Count)
                Cars.RemoveAt(Cars.Count - 1);

            //  힌트 유지: Refresh 때 강제 Clear하지 않고, hinted set대로 보정
            foreach (var id in _hintedCarIds)
            {
                if (_carMap.TryGetValue(id, out var vm))
                    vm.IsExitHinted.Value = true;
            }

            // ---- TrafficLights ----
            var desiredTL = new List<TrafficLightItemViewModel>(dto.TrafficLights.Length);
            var aliveTL = new HashSet<int>();

            for (int i = 0; i < dto.TrafficLights.Length; i++)
            {
                var t = dto.TrafficLights[i];
                aliveTL.Add(t.Id);

                if (!_trafficLightMap.TryGetValue(t.Id, out var vm))
                {
                    vm = new TrafficLightItemViewModel();
                    vm.Id.Value = t.Id;

                    vm.WorldPos.Value = t.WorldPos;
                    vm.LineDir.Value = t.LineDir;
                    vm.LineLength.Value = t.LineLength;

                    vm.InitPhase(t.PhaseOffsetSeconds); 
                    _trafficLightMap[t.Id] = vm;
                }
                else
                {
                    // 설정만 갱신
                    vm.WorldPos.Value = t.WorldPos;
                    vm.LineDir.Value = t.LineDir;
                    vm.LineLength.Value = t.LineLength;

                }
                
                desiredTL.Add(vm);
            }


            for (int i = TrafficLights.Count - 1; i >= 0; i--)
            {
                var vm = TrafficLights[i];

                if (!aliveTL.Contains(vm.Id.Value))
                {
                    TrafficLights.RemoveAt(i);
                    _trafficLightMap.Remove(vm.Id.Value);
                }
            }

            for (int i = 0; i < desiredTL.Count; i++)
            {
                if (i < TrafficLights.Count && ReferenceEquals(TrafficLights[i], desiredTL[i]))
                    continue;

                int found = TrafficLights.IndexOf(desiredTL[i]);

                if (found >= 0)
                {
                    TrafficLights.RemoveAt(found);
                    TrafficLights.Insert(i, desiredTL[i]);
                }
                else { TrafficLights.Insert(i, desiredTL[i]); }
            }

            while (TrafficLights.Count > desiredTL.Count)
                TrafficLights.RemoveAt(TrafficLights.Count - 1);

        }

        public bool HasEditorPreview() => _facade.HasPreviewStageRun;

        public void HardRefreshFromDomain()
        {
            Cars.Clear();
            _carMap.Clear();
            _hintedCarIds.Clear();
            RefreshFromDomain();
        }

        async UniTask TurnOnRandomExitHintOne()
        {
            var candidates = new List<CarItemViewModel>();

            for (int i = 0; i < Cars.Count; i++)
            {
                var vm = Cars[i];
                if (vm.IsLeaving.Value) continue;

                // 이미 힌트 켜진 차 제외
                if (_hintedCarIds.Contains(vm.CarId)) continue;
                if (vm.IsExitHinted.Value) continue;

                // 탈출 가능만 후보
                if (_facade.WouldExit(vm.CarId))
                    candidates.Add(vm);
            }

            if (candidates.Count == 0) { return; }


            var ok = await _facade.TryConsumeBooster(BoosterId.EscapeHint);

            if (!ok)
            {
                await OpenBuyHintCommand.ExecuteAsync();
                return;
            }

            int idx = UnityEngine.Random.Range(0, candidates.Count);
            var picked = candidates[idx];

            picked.IsExitHinted.Value = true;
            _hintedCarIds.Add(picked.CarId);
            await BoosterBarViewModel.EscapeHint.RefreshAsync();
        }

        void ClearExitHintForCar(int carId)
        {
            if (_hintedCarIds.Remove(carId))
            {
                if (_carMap.TryGetValue(carId, out var vm))
                    vm.IsExitHinted.Value = false;
            }
        }

        void ClearAllExitHints()
        {
            if (_hintedCarIds.Count == 0) return;

            foreach (var id in _hintedCarIds)
            {
                if (_carMap.TryGetValue(id, out var vm))
                    vm.IsExitHinted.Value = false;
            }

            _hintedCarIds.Clear();
        }


        public void StartTrafficLightLoop()
        {
            _trafficLightCts?.Cancel();
            _trafficLightCts = new CancellationTokenSource();

            RunTrafficLightLoop(_trafficLightCts.Token).Forget();
        }

        async UniTaskVoid RunTrafficLightLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(TrafficLightTickDt), cancellationToken: ct);

                for (int i = 0; i < TrafficLights.Count; i++)
                    TrafficLights[i].Tick(TrafficLightTickDt);
            }
        }
        
        CancellationTokenSource _cashTruckCts;
        const float CashTickDt = 0.1f;
        readonly TickCashTruckUseCase _tickCashTruck; // DI
        
        public void StartCashTruckLoop()
        {
            _cashTruckCts?.Cancel();
            _cashTruckCts = new CancellationTokenSource();

            RunCashTruckLoop(_cashTruckCts.Token).Forget();
        }

        public void StopCashTruckLoop()
        {
            _cashTruckCts?.Cancel();
            _cashTruckCts = null;
        }


        async UniTaskVoid RunCashTruckLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(CashTickDt), cancellationToken: ct);

                if (Status.Value != StageStatus.Playing) continue;
               // if (_fx is { IsPlaying: true }) continue; // 이동/FX 중이면 멈춤

                _tickCashTruck.Execute(CashTickDt);
                RefreshFromDomain(); // DTO 갱신 → UI 갱신
            }
        }


        public void StopTrafficLightLoop()
        {
            _trafficLightCts?.Cancel();
            _trafficLightCts = null;
        }

        int _penalizedMoveSeq = -1;

        void ApplyTrafficLightPenaltyOncePerMove(TrafficLightPenaltyEvent e)
        {
            if (_penalizedMoveSeq == _moveSeq) return;
            _penalizedMoveSeq = _moveSeq;
            _stateWriter.ApplyExtraMoveCost(e.ExtraCost);
            

            RefreshFromDomain();
        }
        

        public void Dispose()
        {
            _tlPenaltyBinding?.Deregister();
            _tlPenaltyBinding = null;

            if (_fx != null)
                _fx.OnAllFxCompleted -= HandleAllFxCompleted;

            StopTrafficLightLoop();
            StopCashTruckLoop();
        }
        
    }
}