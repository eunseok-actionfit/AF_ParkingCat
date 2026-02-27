using System;
using System.Collections.Generic;
using System.IO;
using AES.Tools;
using ParkingCat.Entities;
using ParkingCat.Factories;
using ParkingCat.Game;
using ParkingCat.Game.Facades;
using ParkingCat.Move.Ports;
using ParkingCat.Repository;
using ParkingCat.ValueObjects;
using ParkingCat.ValueObjects.Level;
using SimpleFileBrowser;
using UnityEngine;


namespace ParkingCat.LevelEditor
{
    public enum EditorStageMode
    {
        Editing,
        Playing
    }

    public sealed class LevelEditorViewModel
    {
        // ===== Save/Load =====
        const string LastStagePathKey = "LEVEL_EDITOR_LAST_PATH";
        const string StageFilePrefix = "stage_";
        public ObservableProperty<int> Stage { get; } = new();

        public ICommand SaveAsJsonCommand { get; }
        public ICommand LoadFromJsonCommand { get; }
        // ====================

        [Bindable]
        public ObservableProperty<LevelConfigViewModel> Current { get; } = new();
        public ObservableProperty<EditorStageMode> Mode { get; } = new();

        public ICommand NewCommand { get; }
        public ICommand RefreshPreviewCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        readonly Stack<Config.StageConfig> _undo = new();
        readonly Stack<Config.StageConfig> _redo = new();

        public ObservableProperty<StageRun> PreviewStageRun { get; } = new();

        readonly GameViewModel _gameVm;
        readonly IGameFacade _facade;
        readonly IStageRunRepository _runRepo;
        readonly IInputGate _inputGate;

        // ===== Play 직전 상태 스냅샷(Stop 시 완전 복원) =====
        Config.StageConfig _snapshotCfgBeforePlay;
        int _snapshotStageBeforePlay;
        List<Config.StageConfig> _snapshotUndoBeforePlay;
        List<Config.StageConfig> _snapshotRedoBeforePlay;
        // ===============================================

        public LevelEditorViewModel(
            GameViewModel gameVm,
            IGameFacade facade,
            IInputGate inputGate,
            IStageRunRepository runRepo)
        {
            _gameVm = gameVm;
            _facade = facade;
            _runRepo = runRepo;
            _inputGate = inputGate;

            _inputGate.Lock();

            NewCommand = new Command(NewStage);
            RefreshPreviewCommand = new Command(RefreshPreview, () => Mode.Value == EditorStageMode.Editing);

            PlayCommand = new Command(() =>
            {
                if (Current.Value == null) return;

                // (스냅샷 저장은 기존대로)
                _snapshotCfgBeforePlay = CloneConfig(Current.Value.ToConfig());
                _snapshotStageBeforePlay = Stage.Value;

                _snapshotUndoBeforePlay = new List<Config.StageConfig>();
                foreach (var u in _undo) _snapshotUndoBeforePlay.Add(CloneConfig(u));

                _snapshotRedoBeforePlay = new List<Config.StageConfig>();
                foreach (var r in _redo) _snapshotRedoBeforePlay.Add(CloneConfig(r));

                // 1) 편집 상태로 프리뷰 생성 + Repo.Reset(run)까지 완료
                RefreshPreview();

                // 2) 플레이는 "프리뷰 dto"가 아니라 "Repo의 StageRun"을 소스로 써야 함
                _facade.ClearPreviewStageRun();
                _gameVm.RefreshFromDomain(); // 초기 상태 1회 동기화(선택이지만 권장)
                _gameVm.StartTrafficLightLoop();
                _gameVm.StartCashTruckLoop();

                Current.Value.SelectedCar.Value = null;

                Mode.Value = EditorStageMode.Playing;
                _inputGate.Unlock();
            }, () => Mode.Value == EditorStageMode.Editing);


            StopCommand = new Command(() =>
            {
                _inputGate.Lock();
                Mode.Value = EditorStageMode.Editing;

                // ===== Play 직전 상태로 "완전" 복원 =====
                if (_snapshotCfgBeforePlay != null)
                {
                    if (Current.Value == null)
                        Current.Value = new LevelConfigViewModel(this);

                    Current.Value.FromConfig(_snapshotCfgBeforePlay);
                    Stage.Value = _snapshotStageBeforePlay;

                    _undo.Clear();
                    _redo.Clear();

                    if (_snapshotUndoBeforePlay != null)
                    {
                        for (int i = _snapshotUndoBeforePlay.Count - 1; i >= 0; i--)
                            _undo.Push(CloneConfig(_snapshotUndoBeforePlay[i]));
                    }

                    if (_snapshotRedoBeforePlay != null)
                    {
                        for (int i = _snapshotRedoBeforePlay.Count - 1; i >= 0; i--)
                            _redo.Push(CloneConfig(_snapshotRedoBeforePlay[i]));
                    }
                }
                // =======================================
                
                RefreshPreview();
                _gameVm.HardRefreshFromDomain();
                _gameVm.StopTrafficLightLoop();
                _gameVm.StopCashTruckLoop();
            }, () => Mode.Value == EditorStageMode.Playing);

            UndoCommand = new Command(Undo, CanUndo);
            RedoCommand = new Command(Redo, CanRedo);

            SaveAsJsonCommand = new Command(OnSaveAsJson, () => Mode.Value == EditorStageMode.Editing && Current.Value != null);
            LoadFromJsonCommand = new Command(OnLoadFromJson, () => Mode.Value == EditorStageMode.Editing);

            NewStage();
        }

        void NewStage()
        {
            var vm = new LevelConfigViewModel(this);
            Current.Value = vm;
            _undo.Clear();
            _redo.Clear();
            Stage.Value = 0;

            // 플레이 스냅샷 무효화
            _snapshotCfgBeforePlay = null;
            _snapshotUndoBeforePlay = null;
            _snapshotRedoBeforePlay = null;

            RefreshPreview();
        }

        public void PushUndo()
        {
            if (Mode.Value != EditorStageMode.Editing) return;
            if (Current.Value == null) return;

            _undo.Push(CloneConfig(Current.Value.ToConfig()));
            _redo.Clear();
        }

        bool CanUndo() => Mode.Value == EditorStageMode.Editing && _undo.Count > 0;
        bool CanRedo() => Mode.Value == EditorStageMode.Editing && _redo.Count > 0;

        void Undo()
        {
            if (!CanUndo() || Current.Value == null) return;

            _redo.Push(CloneConfig(Current.Value.ToConfig()));
            var prev = _undo.Pop();
            Current.Value.FromConfig(prev);
            RefreshPreview();
        }

        void Redo()
        {
            if (!CanRedo() || Current.Value == null) return;

            _undo.Push(CloneConfig(Current.Value.ToConfig()));
            var next = _redo.Pop();
            Current.Value.FromConfig(next);
            RefreshPreview();
        }

        static Config.StageConfig CloneConfig(Config.StageConfig src)
        {
            if (src == null) return null;

            var clone = new Config.StageConfig
            {
                StageId = src.StageId,
                Variant = src.Variant,
                Width = src.Width,
                Height = src.Height,
                Difficulty = src.Difficulty,
                MoveLimit = src.MoveLimit,
                Road01 = (int[])src.Road01?.Clone(),
                Cars = null,
                TrafficLights = null ,
                OneWayLanes = null
            };


            // ===== Cars 복제 (기존) =====
            if (src.Cars != null)
            {
                var cars = new Config.StageConfig.CarConfig[src.Cars.Length];
                for (int i = 0; i < src.Cars.Length; i++)
                {
                    var c = src.Cars[i];
                    cars[i] = new Config.StageConfig.CarConfig
                    {
                        CarId = c.CarId,
                        HeadCell = c.HeadCell,
                        Length = c.Length,
                        Dir = c.Dir,
                        TurnProgram = (TurnAction[])c.TurnProgram?.Clone(),
                        Type = c.Type,
                        EscapeMoveLimit = c.EscapeMoveLimit
                    };
                }
                clone.Cars = cars;
            }
            
            if (src.TrafficLights != null)
            {
                var arr = new Config.StageConfig.TrafficLightConfig[src.TrafficLights.Length];
                for (int i = 0; i < src.TrafficLights.Length; i++)
                {
                    var t = src.TrafficLights[i];
                    arr[i] = new Config.StageConfig.TrafficLightConfig
                    {
                        Id = t.Id,
                        WorldPos = t.WorldPos,
                        LineDir = t.LineDir,
                        PhaseOffsetSeconds = t.PhaseOffsetSeconds
                    };
                }
                clone.TrafficLights = arr;
            }
            
            
            if (src.OneWayLanes != null)
            {
                var arr = new Config.StageConfig.OneWayLaneConfig[src.OneWayLanes.Length];
                for (int i = 0; i < src.OneWayLanes.Length; i++)
                {
                    var o = src.OneWayLanes[i];
                    arr[i] = new Config.StageConfig.OneWayLaneConfig
                    {
                        StartCell = o.StartCell,
                        Dir = o.Dir,
                        Length = o.Length,
                        Thickness = o.Thickness,
                        LaneIndex = o.LaneIndex,
                        AllowedDir = o.AllowedDir
                    };
                }
                clone.OneWayLanes = arr;
            }

            return clone;
        }

        void RefreshPreview()
        {
            if (Current.Value == null) return;
            if (Mode.Value == EditorStageMode.Playing) return;

            var cfg = Current.Value.ToConfig();
            var run = StageFactory.CreateFromConfig(cfg);

            PreviewStageRun.Value = run;

            // TryMove는 Repo에서 StageRun을 꺼내 쓰므로 여기서 반드시 초기화
            _runRepo.Reset(run);

            var dto = ParkingCat.Stage.Mapping.StageRunMapper.ToDto(run);
            _facade.SetPreviewStageRun(dto);

            _gameVm.RefreshFromDomain();
        }

        // =========================
        // Save/Load (StageId: int)
        // =========================

        int ParseStageNumberFromFileName(string fileNameWithoutExt)
        {
            if (string.IsNullOrWhiteSpace(fileNameWithoutExt))
                return 0;

            var s = fileNameWithoutExt.Trim();

            if (s.StartsWith(StageFilePrefix, StringComparison.OrdinalIgnoreCase))
                s = s.Substring(StageFilePrefix.Length);

            return int.TryParse(s, out var id) ? id : 0;
        }

        string MakeDefaultFileName(int stageId) => $"{StageFilePrefix}{stageId}.json";

        string GetInitialDirectory()
        {
            var saved = PlayerPrefs.GetString(LastStagePathKey, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    string dir = null;

                    if (Directory.Exists(saved)) dir = saved;
                    else if (File.Exists(saved)) dir = Path.GetDirectoryName(saved);

                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                        return dir;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"GetInitialDirectory 실패, 기본 경로로 fallback: {e}");
                }
            }

            var fallback = Path.Combine(Application.persistentDataPath, "Stages");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        void SaveLastPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            string dirToSave = null;

            if (Directory.Exists(path)) dirToSave = path;
            else
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    dirToSave = dir;
            }

            if (string.IsNullOrEmpty(dirToSave))
                return;

            PlayerPrefs.SetString(LastStagePathKey, dirToSave);
            PlayerPrefs.Save();
        }

        void OnSaveAsJson()
        {
            var vm = Current.Value;
            if (vm == null) return;

            var config = vm.ToConfig();

            // 현재 Stage 값을 StageId에 반영 (int)
            if (Stage.Value > 0)
                config.StageId = Stage.Value;

            var initialPath = GetInitialDirectory();
            var initialFilename = (config.StageId > 0) ? MakeDefaultFileName(config.StageId) : "NewStage.json";

            FileBrowser.SetFilters(false, new FileBrowser.Filter("JSON", ".json"));
            FileBrowser.SetDefaultFilter(".json");

            FileBrowser.ShowSaveDialog(
                onSuccess: paths =>
                {
                    if (paths == null || paths.Length == 0) return;

                    var path = paths[0];
                    if (string.IsNullOrEmpty(Path.GetExtension(path)))
                        path += ".json";

                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);

                    // 파일명(stage_001)에서 번호 파싱 -> Stage/StageId 갱신
                    var parsedId = ParseStageNumberFromFileName(fileNameWithoutExt);
                    if (parsedId > 0)
                    {
                        Stage.Value = parsedId;
                        config.StageId = parsedId;
                    }

                    var json = JsonUtility.ToJson(config, prettyPrint: true);
                    File.WriteAllText(path, json);

                    SaveLastPath(path);
                },
                onCancel: () => { },
                pickMode: FileBrowser.PickMode.Files,
                initialPath: initialPath,
                initialFilename: initialFilename,
                title: "Save Stage JSON",
                saveButtonText: "Save"
            );
        }

        void OnLoadFromJson()
        {
            var initialPath = GetInitialDirectory();

            FileBrowser.SetFilters(false, new FileBrowser.Filter("JSON", ".json"));
            FileBrowser.SetDefaultFilter(".json");

            FileBrowser.ShowLoadDialog(
                onSuccess: paths =>
                {
                    if (paths == null || paths.Length == 0) return;

                    var path = paths[0];
                    if (!File.Exists(path)) return;

                    SaveLastPath(path);

                    var json = File.ReadAllText(path);
                    var config = JsonUtility.FromJson<Config.StageConfig>(json);
                    if (config == null) return;

                    if (Current.Value == null)
                        Current.Value = new LevelConfigViewModel(this);

                    Current.Value.FromConfig(config);

                    // StageId(int) 우선, 없으면 파일명에서
                    var id = config.StageId;
                    if (id <= 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        id = ParseStageNumberFromFileName(fileName);
                    }
                    Stage.Value = id;

                    _undo.Clear();
                    _redo.Clear();

                    // 로드 직후 플레이 스냅샷은 무효화(새 기준점)
                    _snapshotCfgBeforePlay = null;
                    _snapshotUndoBeforePlay = null;
                    _snapshotRedoBeforePlay = null;

                    RefreshPreview();
                },
                onCancel: () => { },
                pickMode: FileBrowser.PickMode.Files,
                initialPath: initialPath,
                title: "Load Stage JSON",
                loadButtonText: "Load"
            );
        }
    }
}
