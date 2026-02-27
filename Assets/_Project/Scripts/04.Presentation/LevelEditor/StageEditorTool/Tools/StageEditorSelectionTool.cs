using System;
using ParkingCat.LevelEditor.StageEditorTool.Context;
using ParkingCat.LevelEditor.Util;
using UnityEngine;


namespace ParkingCat.LevelEditor.StageEditorTool.Tools
{
    public sealed class StageEditorSelectionTool : IStageEditorTool
    {
        enum ToolState { Idle, Selecting, Selected, DraggingSelection }

        ToolState _state = ToolState.Idle;

        Vector2Int _anchorCell;
        RectInt _selection;
        bool _hasSelection;

        int[] _selBuffer;
        int _selW, _selH;

        Vector2Int _dragStartCell;
        RectInt _dragStartSelection;
        bool _dragCopy;

        Vector2Int _lastLiveDelta = new Vector2Int(int.MinValue, int.MinValue);
        int[] _dragBaseRoad;
        int _dragBaseW, _dragBaseH;

        public bool HasSelection => _hasSelection;
        public RectInt Selection => _selection;

        public void OnEnter(StageEditorToolContext ctx)
        {
            // keep selection as-is (handy when toggling tools); no-op
        }

        public void OnExit(StageEditorToolContext ctx)
        {
            // keep selection; router may choose to clear explicitly
        }

        public void Clear()
        {
            _state = ToolState.Idle;
            _hasSelection = false;
            _selBuffer = null;
            _dragBaseRoad = null;
        }

        public void Tick(StageEditorToolContext ctx)
        {
            var cfg = ctx.Cfg;
            if (cfg == null) return;

            // Mouse down
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (!ctx.TryGetMouseCell(out var cell))
                    return;

                if (_hasSelection && _selection.Contains(cell))
                {
                    _state = ToolState.DraggingSelection;
                    ctx.PushUndo?.Invoke();
                    _dragStartCell = cell;
                    _dragStartSelection = _selection;
                    _dragCopy = ctx.IsCtrlDown != null && ctx.IsCtrlDown.Invoke();
                    _lastLiveDelta = new Vector2Int(int.MinValue, int.MinValue);

                    CacheSelectionBuffer(ctx);

                    _dragBaseW = cfg.Width.Value;
                    _dragBaseH = cfg.Height.Value;
                    var arr = cfg.Road01.Value ?? Array.Empty<int>();
                    _dragBaseRoad = (int[])arr.Clone();
                }
                else
                {
                    _state = ToolState.Selecting;
                    _anchorCell = cell;
                    _selection = new RectInt(cell.x, cell.y, 1, 1);
                    _hasSelection = true;
                }
            }

            // Selecting drag
            if (UnityEngine.Input.GetMouseButton(0) && _state == ToolState.Selecting)
            {
                if (!ctx.TryGetMouseCell(out var cell))
                    return;

                _selection = EditorGridOps.MakeRectFromTwoCells(_anchorCell, cell);
            }

            // Live drag move: redraw tilemap only
            if (_state == ToolState.DraggingSelection && UnityEngine.Input.GetMouseButton(0))
            {
                if (ctx.TryGetMouseCell(out var cell))
                {
                    var delta = cell - _dragStartCell;
                    ApplyMoveLive_TileOnly(ctx, delta.x, delta.y);
                }
            }

            // Mouse up
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (_state == ToolState.Selecting)
                {
                    _state = ToolState.Selected;
                    CacheSelectionBuffer(ctx);
                }
                else if (_state == ToolState.DraggingSelection)
                {
                    CommitLiveDragAndRefreshPreview(ctx);

                    _state = ToolState.Selected;
                    CacheSelectionBuffer(ctx);
                }
            }

            // Right click: clear selection
            if (UnityEngine.Input.GetMouseButtonDown(1))
                Clear();

            // Delete: clear selection area
            if (_hasSelection && (UnityEngine.Input.GetKeyDown(KeyCode.Delete) || UnityEngine.Input.GetKeyDown(KeyCode.Backspace)))
            {
                FillRect(ctx, _selection, 0);
                ctx.RefreshPreview?.Invoke();
                CacheSelectionBuffer(ctx);
            }
        }

        public void UpdateSelectionLine(StageEditorToolContext ctx)
        {
            if (ctx.SelectionLine == null) return;

            if (!_hasSelection)
            {
                ctx.SelectionLine.enabled = false;
                return;
            }

            ctx.SelectionLine.enabled = true;

            var corners = new Vector3[4];
            ctx.GridView.RectCornerWorlds(_selection, corners);

            for (int i = 0; i < 4; i++)
                corners[i].z = ctx.SelectionZ;

            ctx.SelectionLine.positionCount = 5;
            ctx.SelectionLine.loop = false;

            ctx.SelectionLine.SetPosition(0, corners[0]);
            ctx.SelectionLine.SetPosition(1, corners[1]);
            ctx.SelectionLine.SetPosition(2, corners[2]);
            ctx.SelectionLine.SetPosition(3, corners[3]);
            ctx.SelectionLine.SetPosition(4, corners[0]);
        }

        void CacheSelectionBuffer(StageEditorToolContext ctx)
        {
            if (!_hasSelection)
            {
                _selBuffer = null;
                return;
            }

            _selW = Mathf.Max(1, _selection.width);
            _selH = Mathf.Max(1, _selection.height);
            _selBuffer = EditorGridOps.CopyRect(ctx.Cfg.Road01.Value, ctx.Cfg.Width.Value, ctx.Cfg.Height.Value, _selection);
        }

        void ApplyMoveLive_TileOnly(StageEditorToolContext ctx, int dx, int dy)
        {
            if (!_hasSelection) return;

            if (_lastLiveDelta.x == dx && _lastLiveDelta.y == dy) return;
            _lastLiveDelta = new Vector2Int(dx, dy);

            var cfg = ctx.Cfg;

            if (_dragBaseRoad == null || cfg.Width.Value != _dragBaseW || cfg.Height.Value != _dragBaseH)
            {
                ApplyMoveFallback(ctx, dx, dy, copy: _dragCopy);
                return;
            }

            if (_selBuffer == null || _selBuffer.Length != _selW * _selH)
                CacheSelectionBuffer(ctx);

            var arr = cfg.Road01.Value;

            if (arr == null || arr.Length != _dragBaseRoad.Length)
            {
                arr = (int[])_dragBaseRoad.Clone();
                cfg.Road01.Value = arr;
            }
            else
            {
                Array.Copy(_dragBaseRoad, arr, _dragBaseRoad.Length);
            }

            int w = cfg.Width.Value;
            int h = cfg.Height.Value;

            if (!_dragCopy)
                EditorGridOps.FillRectDirect(arr, w, h, _dragStartSelection, 0);

            var target = new RectInt(
                _dragStartSelection.x + dx,
                _dragStartSelection.y + dy,
                _dragStartSelection.width,
                _dragStartSelection.height
            );

            EditorGridOps.PasteRectDirect(arr, w, h, target, _selW, _selH, _selBuffer, ctx.OverwriteZero);

            _selection = target;
            _hasSelection = true;

            ctx.DrawGridFromRoad01?.Invoke(arr, w, h);
        }

        void CommitLiveDragAndRefreshPreview(StageEditorToolContext ctx)
        {
            var cfg = ctx.Cfg;
            cfg.Road01.Value = cfg.Road01.Value; // force notify

            ctx.RefreshPreview?.Invoke();
          //  ctx.FitCameraToBoard(true);

            _dragBaseRoad = null;
        }

        void ApplyMoveFallback(StageEditorToolContext ctx, int dx, int dy, bool copy)
        {
            if (!_hasSelection) return;
            if (dx == 0 && dy == 0) return;

            if (_selBuffer == null || _selBuffer.Length != _selW * _selH)
                CacheSelectionBuffer(ctx);

            if (!copy)
                FillRect(ctx, _dragStartSelection, 0);

            var target = new RectInt(
                _dragStartSelection.x + dx,
                _dragStartSelection.y + dy,
                _dragStartSelection.width,
                _dragStartSelection.height
            );

            PasteRect(ctx, target, _selW, _selH, _selBuffer, overwriteZero: ctx.OverwriteZero);

            _selection = target;
            _hasSelection = true;

            ctx.RefreshPreview?.Invoke();
        }

        static void PasteRect(StageEditorToolContext ctx, RectInt r, int w, int h, int[] buf, bool overwriteZero)
        {
            var cfg = ctx.Cfg;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int v = buf[y * w + x];
                    if (!overwriteZero && v == 0) continue;

                    int tx = r.x + x;
                    int ty = r.y + y;

                    if (!cfg.InBounds(tx, ty)) continue;
                    cfg.SetCell(tx, ty, v);
                }
        }

        static void FillRect(StageEditorToolContext ctx, RectInt r, int v01)
        {
            var cfg = ctx.Cfg;

            for (int y = 0; y < r.height; y++)
                for (int x = 0; x < r.width; x++)
                    cfg.SetCell(r.x + x, r.y + y, v01);
        }
    }
}
