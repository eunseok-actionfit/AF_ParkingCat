using System;
using AES.Tools;
using ParkingCat.Config;
using ParkingCat.LevelEditor.Util;
using ParkingCat.ValueObjects.Level;
using UnityEngine;


// TurnAction, TurnProgram 등


namespace ParkingCat.LevelEditor
{
    public enum BoardResizeDir
    {
        Up, Down, Left,
        Right
    }

    public sealed class LevelConfigViewModel
    {
        int _roadW = -1;
        int _roadH = -1;

        // ★ FromConfig/복원 중간 단계에서 Ensure/Prune가 끼어들지 않게 막는 플래그
        bool _suspendAutoEnsure;

        // 식별(에디터 저장/로드용)
        public ObservableProperty<int> StageId { get; } = new(1);
        public ObservableProperty<int> Variant { get; } = new(0);

        // Grid
        public ObservableProperty<int> Width { get; } = new(4);
        public ObservableProperty<int> Height { get; } = new(7);

        // row-major (y*w + x), 1=road 0=wall
        public ObservableProperty<int[]> Road01 { get; } = new(null);

        // Rules
        public ObservableProperty<LevelDifficulty> Difficulty { get; } = new();
        public ObservableProperty<int> MoveLimit { get; } = new(-1); // -1 = 무한

        // Cars
        public ObservableList<CarConfigViewModel> Cars { get; } = new();

        public ObservableList<TrafficLightConfigViewModel> TrafficLights { get; } = new();
        
        public ObservableList<OneWayLaneConfigViewModel> OneWayLanes { get; } = new();

        // Selection
        public ObservableProperty<CarConfigViewModel> SelectedCar { get; } = new(null);
        public ObservableProperty<int> SelectedCarIndex { get; } = new(-1);
        public ObservableProperty<int> SelectedTrafficLightIndex { get; } = new(-1);

        public ICommand AddCarCommand { get; }
        public ICommand RemoveSelectedCarCommand { get; }
        public ICommand AddTrafficLightCommand { get; }
        public ICommand RemoveSelectedTrafficLightCommand { get; }
        
        public LevelEditorViewModel parent;

        public LevelConfigViewModel(LevelEditorViewModel parent)
        {
            this.parent = parent;
            EnsureRoadAllocated();

            AddCarCommand = new Command(AddCar);

            RemoveSelectedCarCommand = new Command(
                execute: RemoveSelectedCar,
                canExecute: () =>
                {
                    var idx = SelectedCarIndex.Value;
                    return idx >= 0 && idx < Cars.Count;
                });

            AddTrafficLightCommand = new Command(AddTrafficLight);

            RemoveSelectedTrafficLightCommand = new Command(
                execute: RemoveSelectedTrafficLight,
                canExecute: () =>
                {
                    var idx = SelectedTrafficLightIndex.Value;
                    return idx >= 0 && idx < TrafficLights.Count;
                });

            Width.Subscribe(_ => EnsureRoadAllocated());
            Height.Subscribe(_ => EnsureRoadAllocated());

            SelectedCarIndex.OnValueChanged += _ =>
            {
                SelectedCar.Value = CarPlacement.GetSelectedCar(this);
            };
        }

        public void EnsureRoadAllocated()
        {
            if (_suspendAutoEnsure)
                return;

            int newW = Math.Max(1, Width.Value);
            int newH = Math.Max(1, Height.Value);

            if (_roadW < 1) _roadW = newW;
            if (_roadH < 1) _roadH = newH;

            var oldArr = Road01.Value;

            Width.Value = newW;
            Height.Value = newH;

            int newN = newW * newH;

            if (oldArr == null || oldArr.Length == 0)
            {
                Road01.Value = new int[newN];
                _roadW = newW;
                _roadH = newH;
                return;
            }

            if (oldArr.Length == newN)
            {
                _roadW = newW;
                _roadH = newH;
                return;
            }

            // 보존 리사이즈
            var oldW = _roadW;
            var oldH = _roadH;

            var next = EditorGridOps.ResizePreserve(oldArr, oldW, oldH, newW, newH, fill01: 0);
            Road01.Value = next;

            _roadW = newW;
            _roadH = newH;

            // ★ 축소일 때만 prune
            if (newW < oldW || newH < oldH)
                PruneCarsOutOfBounds();
        }

        public int GetCell(int x, int y)
        {
            if (!InBounds(x, y)) return 0;
            return Road01.Value[y * Width.Value + x];
        }

        public void TouchRoad() => Road01.Value = Road01.Value;

        public void SetCell(int x, int y, int v01)
        {
            if (!InBounds(x, y)) return;
            EditorGridOps.Set(Road01.Value, Width.Value, Height.Value, x, y, v01);

            PruneOneWayLanesIfHiddenByRoad(); 

            TouchRoad();
        }


        public bool FloodFillCell(int x, int y, int v01)
        {
            if (!InBounds(x, y)) return false;
            var arr = Road01.Value;
            bool changed = EditorGridOps.FloodFill(arr, Width.Value, Height.Value, x, y, v01);
            if (changed)
            {
                PruneOneWayLanesIfHiddenByRoad(); // ★ 추가
                TouchRoad();
            }
            return changed;
        }


        public bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < Width.Value && y < Height.Value;

        void AddCar()
        {
            var vm = new CarConfigViewModel(() => parent.RefreshPreviewCommand.Execute());
            vm.CarId.Value = NextCarId();
            vm.HeadCell.Value = new Vector2Int(0, 0);
            vm.Length.Value = 2;
            vm.Dir.Value = Dir4.Right;

            Cars.Add(vm);
            SelectedCarIndex.Value = Cars.Count - 1;
        }

        int NextCarId()
        {
            int max = 0;

            for (int i = 0; i < Cars.Count; i++)
                max = Math.Max(max, Cars[i].CarId.Value);

            return max + 1;
        }

        void RemoveSelectedCar()
        {
            var idx = SelectedCarIndex.Value;
            if (idx < 0 || idx >= Cars.Count) return;

            Cars.RemoveAt(idx);

            if (Cars.Count == 0) SelectedCarIndex.Value = -1;
            else SelectedCarIndex.Value = Math.Min(idx, Cars.Count - 1);
        }

        void AddTrafficLight()
        {
        
            int nextId = 1;
            for (int i = 0; i < TrafficLights.Count; i++)
                nextId = Math.Max(nextId, TrafficLights[i].Id.Value + 1);

            var vm = new TrafficLightConfigViewModel();
            vm.Id.Value = nextId;
            vm.WorldPos.Value = Vector2.zero;
            vm.LineDir.Value = Dir4.Right;
            vm.LineLength.Value = 1;
            vm.PhaseOffsetSeconds.Value = 0f;

            TrafficLights.Add(vm);
            SelectedTrafficLightIndex.Value = TrafficLights.Count - 1;
        }

        int NextTrafficLightId()
        {
            int max = 0;

            for (int i = 0; i < TrafficLights.Count; i++)
                max = Math.Max(max, TrafficLights[i].Id.Value);

            return max + 1;
        }

        void RemoveSelectedTrafficLight()
        {
            var idx = SelectedTrafficLightIndex.Value;
            if (idx < 0 || idx >= TrafficLights.Count) return;

            TrafficLights.RemoveAt(idx);
            SelectedTrafficLightIndex.Value = -1;
        }

        // --- VM -> Domain Config ---
        public StageConfig ToConfig()
        {
            EnsureRoadAllocated();

            var cfg = new StageConfig
            {
                StageId = StageId.Value,
                Variant = Variant.Value,
                Width = Width.Value,
                Height = Height.Value,
                Road01 = Road01.Value,
                Difficulty = Difficulty.Value,
                MoveLimit = MoveLimit.Value,
                Cars = BuildCars(),
                TrafficLights = BuildTrafficLights(),
                OneWayLanes = BuildOneWayLanes()
            };

            return cfg;
        }

        StageConfig.CarConfig[] BuildCars()
        {
            if (Cars.Count == 0) return Array.Empty<StageConfig.CarConfig>();

            var arr = new StageConfig.CarConfig[Cars.Count];

            for (int i = 0; i < Cars.Count; i++)
                arr[i] = Cars[i].ToConfig();

            return arr;
        }

        StageConfig.TrafficLightConfig[] BuildTrafficLights()
        {
            if (TrafficLights.Count == 0)
                return Array.Empty<StageConfig.TrafficLightConfig>();

            var arr = new StageConfig.TrafficLightConfig[TrafficLights.Count];
            for (int i = 0; i < TrafficLights.Count; i++)
                arr[i] = TrafficLights[i].ToConfig();

            return arr;
        }
        StageConfig.OneWayLaneConfig[] BuildOneWayLanes()
        {
            if (OneWayLanes.Count == 0)
                return Array.Empty<StageConfig.OneWayLaneConfig>();

            var arr = new StageConfig.OneWayLaneConfig[OneWayLanes.Count];
            for (int i = 0; i < OneWayLanes.Count; i++)
                arr[i] = OneWayLanes[i].ToConfig();

            return arr;
        }

        public void FromConfig(StageConfig cfg)
        {
            _suspendAutoEnsure = true;

            try
            {
                StageId.Value = cfg.StageId;
                Variant.Value = cfg.Variant;

                Width.Value = cfg.Width;
                Height.Value = cfg.Height;
                Road01.Value = cfg.Road01;

                Difficulty.Value = cfg.Difficulty;
                MoveLimit.Value = cfg.MoveLimit;

                Cars.Clear();

                if (cfg.Cars != null)
                {
                    for (int i = 0; i < cfg.Cars.Length; i++)
                    {
                        var vm = new CarConfigViewModel(() => parent.RefreshPreviewCommand.Execute());
                        vm.FromConfig(cfg.Cars[i]);
                        Cars.Add(vm);
                    }
                }

                SelectedCarIndex.Value = -1;
                
                TrafficLights.Clear();

                if (cfg.TrafficLights != null)
                {
                    for (int i = 0; i < cfg.TrafficLights.Length; i++)
                    {
                        var vm = new TrafficLightConfigViewModel();
                        vm.FromConfig(cfg.TrafficLights[i]);
                        TrafficLights.Add(vm);
                    }
                }

                SelectedTrafficLightIndex.Value = TrafficLights.Count > 0 ? 0 : -1;
                
                OneWayLanes.Clear();
                if (cfg.OneWayLanes != null)
                {
                    for (int i = 0; i < cfg.OneWayLanes.Length; i++)
                    {
                        var vm = new OneWayLaneConfigViewModel();
                        vm.FromConfig(cfg.OneWayLanes[i]);
                        OneWayLanes.Add(vm);
                    }
                }


                _roadW = Math.Max(1, Width.Value);
                _roadH = Math.Max(1, Height.Value);
            }
            finally { _suspendAutoEnsure = false; }

            // 로드 완료 후 보정은 하되, prune는 하지 않는다
            EnsureRoadAllocated();
        }


        public void ResizePreserveRoad(int newW, int newH, int fill01 = 0)
        {
            int oldW = _roadW > 0 ? _roadW : Math.Max(1, Width.Value);
            int oldH = _roadH > 0 ? _roadH : Math.Max(1, Height.Value);

            newW = Math.Max(1, newW);
            newH = Math.Max(1, newH);

            var next = EditorGridOps.ResizePreserve(Road01.Value, oldW, oldH, newW, newH, fill01);

            // 순서 중요: 값 세팅 후 배열 재할당(변경 알림 확실)
            Width.Value = newW;
            Height.Value = newH;
            Road01.Value = next;

            _roadW = newW;
            _roadH = newH;

            PruneCarsOutOfBounds();
            PruneTrafficLightsOutOfBounds();
        }

        public void PruneCarsOutOfBounds()
        {
            if (Cars.Count == 0)
            {
                SelectedCarIndex.Value = -1;
                return;
            }

            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                var car = Cars[i];

                if (!CarPlacement.CanPlace(this, car, requireRoadUnderCar: false))
                    Cars.RemoveAt(i);
            }

            // keep selection valid
            if (Cars.Count == 0)
            {
                SelectedCarIndex.Value = -1;
                return;
            }

            int sel = SelectedCarIndex.Value;

            if (sel < 0) SelectedCarIndex.Value = 0;
            else if (sel >= Cars.Count) SelectedCarIndex.Value = Cars.Count - 1;
        }

        void PruneTrafficLightsOutOfBounds()
        {
            if (TrafficLights.Count == 0)
            {
                SelectedTrafficLightIndex.Value = -1;
                return;
            }

            float minX = 0f;
            float minY = 0f;
            float maxX = Width.Value;   // world 0..W
            float maxY = Height.Value;  // world 0..H

            for (int i = TrafficLights.Count - 1; i >= 0; i--)
            {
                var p = TrafficLights[i].WorldPos.Value;

                // “보드 밖” 정의: [0, W) / [0, H) 밖
                // (원하면 <=0.5 같은 정책으로 바꿀 수 있음)
                if (p.x < minX || p.y < minY || p.x >= maxX || p.y >= maxY)
                    TrafficLights.RemoveAt(i);
            }

            if (TrafficLights.Count == 0)
            {
                SelectedTrafficLightIndex.Value = -1;
                return;
            }

            int sel = SelectedTrafficLightIndex.Value;
            if (sel < 0) SelectedTrafficLightIndex.Value = 0;
            else if (sel >= TrafficLights.Count) SelectedTrafficLightIndex.Value = TrafficLights.Count - 1;
        }


        public void ResizeWithDirection(int delta, BoardResizeDir dir, int fill01 = 0)
        {
            int oldW = Width.Value;
            int oldH = Height.Value;

            // 목표 사이즈(1칸 이하로는 못 줄이니 clamp)
            int targetW = oldW;
            int targetH = oldH;

            switch (dir)
            {
                case BoardResizeDir.Up:
                case BoardResizeDir.Down:
                    targetH = Mathf.Max(1, oldH + delta);
                    break;

                case BoardResizeDir.Left:
                case BoardResizeDir.Right:
                    targetW = Mathf.Max(1, oldW + delta);
                    break;
            }

            // clamp로 인해 실제 적용 delta가 달라질 수 있으니 actualDelta를 사용
            int actualDeltaW = targetW - oldW;
            int actualDeltaH = targetH - oldH;

            // 변화 없음
            if (actualDeltaW == 0 && actualDeltaH == 0)
                return;

            int offsetX = 0;
            int offsetY = 0;

            // 핵심: Down/Left는 확장/축소 모두 "방향 쪽"으로 붙도록 offset/오브젝트를 대칭 처리
            switch (dir)
            {
                case BoardResizeDir.Up:
                    // 기존 유지: 아래쪽(혹은 반대쪽)으로만 늘고/줄도록(앵커 고정)
                    break;

                case BoardResizeDir.Down:
                    offsetY = actualDeltaH;
                    OffsetCars(0, actualDeltaH);
                    OffsetTrafficLights(0, actualDeltaH);
                    OffsetOneWayLanes(0, actualDeltaH);
                    break;

                case BoardResizeDir.Left:
                    offsetX = actualDeltaW;
                    OffsetCars(actualDeltaW, 0);
                    OffsetTrafficLights(actualDeltaW, 0);
                    OffsetOneWayLanes(actualDeltaW, 0);
                    break;

                case BoardResizeDir.Right:
                    // 기존 유지
                    break;
            }

            var next = EditorGridOps.ResizePreserveWithOffset(
                Road01.Value,
                oldW, oldH,
                targetW, targetH,
                offsetX, offsetY,
                fill01
            );

            Width.Value = targetW;
            Height.Value = targetH;
            Road01.Value = next;

            _roadW = targetW;
            _roadH = targetH;

            PruneCarsOutOfBounds();
            PruneTrafficLightsOutOfBounds();
        }


        void OffsetCars(int dx, int dy)
        {
            for (int i = 0; i < Cars.Count; i++)
            {
                var car = Cars[i];
                car.HeadCell.Value += new Vector2Int(dx, dy);
            }
        }

        void OffsetTrafficLights(int dx, int dy)
        {
            if (TrafficLights.Count == 0) return;

            var delta = new Vector2(dx, dy);
            for (int i = 0; i < TrafficLights.Count; i++)
            {
                var tl = TrafficLights[i];
                tl.WorldPos.Value += delta;
            }
        }
        
        void OffsetOneWayLanes(int dx, int dy)
        {
            if (OneWayLanes == null || OneWayLanes.Count == 0) return;

            for (int i = 0; i < OneWayLanes.Count; i++)
            {
                var ow = OneWayLanes[i];
                ow.StartCell.Value += new Vector2Int(dx, dy);
            }
        }


        public bool PruneOneWayLanesIfHiddenByRoad()
        {
            if (OneWayLanes == null || OneWayLanes.Count == 0) return false;

            int w = Width.Value;
            int h = Height.Value;
            var road01 = Road01.Value;
            if (road01 == null || road01.Length != w * h) return false;

            bool removed = false;

            bool IsRoad(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return false;
                return road01[y * w + x] == 1;
            }

            // "시각적으로 보이려면": 해당 OneWay lane의 모든 셀이 road(1)여야 한다
            bool IsFootprintAllRoad(OneWayLaneConfigViewModel ow)
            {
                var start = ow.StartCell.Value;
                var dir = ow.Dir.Value;
                int len = ow.Length.Value;
                int th = ow.Thickness.Value;
                int laneIndex = ow.LaneIndex.Value;

                if (len <= 0 || th <= 0) return false;
                if (laneIndex < 0 || laneIndex >= th) return false;

                bool isH = (dir == Dir4.Right || dir == Dir4.Left);
                int step = (dir == Dir4.Left || dir == Dir4.Down) ? -1 : +1;

                if (isH)
                {
                    int y = start.y + laneIndex;
                    int x0 = start.x;

                    for (int i = 0; i < len; i++)
                    {
                        int x = x0 + step * i;
                        if (!IsRoad(x, y)) return false;
                    }
                    return true;
                }
                else
                {
                    int x = start.x + laneIndex;
                    int y0 = start.y;

                    for (int i = 0; i < len; i++)
                    {
                        int y = y0 + step * i;
                        if (!IsRoad(x, y)) return false;
                    }
                    return true;
                }
            }

            for (int i = OneWayLanes.Count - 1; i >= 0; i--)
            {
                var ow = OneWayLanes[i];
                if (ow == null) { OneWayLanes.RemoveAt(i); removed = true; continue; }

                if (!IsFootprintAllRoad(ow))
                {
                    OneWayLanes.RemoveAt(i);
                    removed = true;
                }
            }

            return removed;
        }

    }
}