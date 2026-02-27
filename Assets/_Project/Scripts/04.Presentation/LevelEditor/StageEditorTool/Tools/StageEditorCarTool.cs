// StageEditorCarTool.cs
using ParkingCat.Entities;
using ParkingCat.LevelEditor.StageEditorTool.Context;
using ParkingCat.LevelEditor.Util;
using UnityEngine;


namespace ParkingCat.LevelEditor.StageEditorTool.Tools
{
    public sealed class StageEditorCarTool : IStageEditorTool
    {
        CarType _placeType = CarType.Normal; // 현재 배치할 차 타입(브러시)

        public void OnEnter(StageEditorToolContext ctx) { }
        public void OnExit(StageEditorToolContext ctx) { }

        public void Tick(StageEditorToolContext ctx)
        {
            var cfg = ctx.Cfg;
            if (cfg == null) return;

            // Select / Place
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (!ctx.TryGetMouseCell(out var cell)) return;

                int hitIdx = CarPlacement.FindCarIndexAtCell(cfg, cell);

                if (hitIdx >= 0)
                {
                    cfg.SelectedCarIndex.Value = hitIdx;
                }
                else
                {
                    ctx.PushUndo?.Invoke();
                    cfg.AddCarCommand?.Execute();
                    int idx = cfg.SelectedCarIndex.Value;

                    if (idx < 0 || idx >= cfg.Cars.Count)
                    {
                        var vm = new CarConfigViewModel(() => ctx.RefreshPreview?.Invoke());
                        vm.CarId.Value = CarPlacement.NextCarId(cfg);
                        vm.HeadCell.Value = cell;
                        vm.Length.Value = ctx.CarMinLength;
                        vm.Dir.Value = ctx.NewCarDefaultDir;
                        vm.Type.Value = _placeType;
                        cfg.Cars.Add(vm);
                        cfg.SelectedCarIndex.Value = cfg.Cars.Count - 1;
                        idx = cfg.SelectedCarIndex.Value;
                    }

                    var car = cfg.Cars[idx];
                    car.HeadCell.Value = cell;
                    car.Dir.Value = ctx.NewCarDefaultDir;
                    car.Length.Value = Mathf.Clamp(car.Length.Value, ctx.CarMinLength, ctx.CarMaxLength);
                    
                    car.Type.Value = _placeType;

                    if (ctx.RequireRoadUnderCar && !CarPlacement.CanPlace(cfg, car))
                    {
                        cfg.Cars.RemoveAt(idx);
                        cfg.SelectedCarIndex.Value = cfg.Cars.Count > 0 ? Mathf.Clamp(idx, 0, cfg.Cars.Count - 1) : -1;
                    }

                    ctx.RefreshPreview?.Invoke();
                }
            }

            // Remove
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                if (!ctx.TryGetMouseCell(out var cell)) return;

                int hitIdx = CarPlacement.FindCarIndexAtCell(cfg, cell);

                if (hitIdx >= 0)
                {
                    cfg.SelectedCarIndex.Value = hitIdx;
                    ctx.PushUndo?.Invoke();
                    cfg.RemoveSelectedCarCommand?.Execute();
                    ctx.RefreshPreview?.Invoke();
                }
            }

            // R : Rotate
            if (UnityEngine.Input.GetKeyDown(ctx.CarRotateKey))
            {
                var car = CarPlacement.GetSelectedCar(cfg);
                if (car != null)
                {
                    ctx.PushUndo?.Invoke();

                    car.Dir.Value = Dir4Util.NextCW(car.Dir.Value);

                    if (ctx.RequireRoadUnderCar && !CarPlacement.CanPlace(cfg, car))
                        car.Dir.Value = Dir4Util.PrevCW(car.Dir.Value);

                    ctx.RefreshPreview?.Invoke();
                }
            }

            // T : Turn action (TurnMode)
            if (UnityEngine.Input.GetKeyDown(ctx.CarTurnKey))
            {
                var car = CarPlacement.GetSelectedCar(cfg);
                if (car != null)
                {
                    ctx.PushUndo?.Invoke();

                    bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                    car.TurnMode.Value = shift
                        ? CarPlacement.PrevTurnMode(car.TurnMode.Value)
                        : CarPlacement.NextTurnMode(car.TurnMode.Value);

                    ctx.RefreshPreview?.Invoke();
                }
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1))
            {
                _placeType = CarType.Normal;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2))
            {
                _placeType = CarType.Ambulance;
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad3))
            {
                _placeType = CarType.CashTruck;
            }
        }
    }
}
