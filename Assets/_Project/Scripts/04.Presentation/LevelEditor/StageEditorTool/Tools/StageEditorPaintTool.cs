using ParkingCat.LevelEditor.StageEditorTool.Context;
using UnityEngine;


namespace ParkingCat.LevelEditor.StageEditorTool.Tools
{
    public sealed class StageEditorPaintTool : IStageEditorTool
    {
        bool _strokeActive;
        int _strokeButton = -1;
        int _lastX = int.MinValue;
        int _lastY = int.MinValue;
        int _lastV = int.MinValue;

        public void OnEnter(StageEditorToolContext ctx)
        {
            _lastX = _lastY = _lastV = int.MinValue;
            _strokeActive = false;
            _strokeButton = -1;
        }

        public void OnExit(StageEditorToolContext ctx) { }

        
        public void Tick(StageEditorToolContext ctx)
        {
            if (ctx?.Cfg == null) return;

            // Stroke begin
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _strokeActive = true;
                _strokeButton = 0;
                ctx.PushUndo?.Invoke();

                // Shift+Click: flood fill to road(1)
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
                {
                    if (ctx.TryGetMouseCell(out var c))
                    {
                        ctx.Cfg.FloodFillCell(c.x, c.y, 1);
                        ctx.RefreshPreview?.Invoke();
                    }
                }
            }
            else if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                _strokeActive = true;
                _strokeButton = 1;
                ctx.PushUndo?.Invoke();

                // Shift+Click: flood fill to wall(0)
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
                {
                    if (ctx.TryGetMouseCell(out var c))
                    {
                        ctx.Cfg.FloodFillCell(c.x, c.y, 0);
                        ctx.RefreshPreview?.Invoke();
                    }
                }
            }

            // Stroke end
            if (_strokeActive)
            {
                if ((_strokeButton == 0 && UnityEngine.Input.GetMouseButtonUp(0)) ||
                    (_strokeButton == 1 && UnityEngine.Input.GetMouseButtonUp(1)))
                {
                    _strokeActive = false;
                    _strokeButton = -1;
                    _lastX = _lastY = _lastV = int.MinValue;
                }
            }

            // Normal paint (no shift flood)
            if (UnityEngine.Input.GetMouseButton(0) && !(UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)))
                Paint(ctx, 1);

            if (UnityEngine.Input.GetMouseButton(1) && !(UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)))
                Paint(ctx, 0);
        }

        void Paint(StageEditorToolContext ctx, int v01)
        {
            if (!ctx.TryGetMouseCell(out var cell))
                return;

            if (cell.x == _lastX && cell.y == _lastY && v01 == _lastV)
                return;

            _lastX = cell.x;
            _lastY = cell.y;
            _lastV = v01;

            ctx.Cfg.SetCell(cell.x, cell.y, v01);
            ctx.RefreshPreview?.Invoke();
        }
    }
}
