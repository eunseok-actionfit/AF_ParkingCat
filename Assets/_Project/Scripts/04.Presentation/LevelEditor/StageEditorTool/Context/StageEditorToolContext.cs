// StageEditorToolContext.cs
using System;
using ParkingCat.Game.Grid;
using UnityEngine;


namespace ParkingCat.LevelEditor.StageEditorTool.Context
{
    /// <summary>Shared dependencies & helper functions for editor tools.</summary>
    public sealed class StageEditorToolContext
    {
        public readonly LevelEditorViewModel Editor;
        public readonly Camera Cam;
        public readonly GridTilemapView GridView;
        // public readonly BoardCameraController CameraController;

        // selection visuals
        public readonly LineRenderer SelectionLine;
        public readonly float SelectionZ;

        // config knobs
        public readonly bool OverwriteZero;
        public readonly int WheelStep;

        // car knobs
        public readonly int CarMinLength;
        public readonly int CarMaxLength;
        public readonly Dir4 NewCarDefaultDir;
        public readonly bool RequireRoadUnderCar;
        public readonly KeyCode CarRotateKey;
        public readonly KeyCode CarTurnKey;
        

        public Action RefreshPreview;
        public Action PushUndo;
        public Action<int[], int, int> DrawGridFromRoad01; // (road01, w, h)
        public Func<bool> IsCtrlDown;
        public Func<bool> IsAltDown;
        
        public Action<string> SetHintText;  
        public Action<bool> SetHintVisible; 

        public StageEditorToolContext(
            LevelEditorViewModel editor,
            Camera cam,
            GridTilemapView gridView,
            LineRenderer selectionLine,
            float selectionZ,
            bool overwriteZero,
            int wheelStep,
            int carMinLength,
            int carMaxLength,
            Dir4 newCarDefaultDir,
            bool requireRoadUnderCar,
            KeyCode carRotateKey,
            KeyCode carTurnKey)
        {
            Editor = editor;
            Cam = cam;
            GridView = gridView;
            // CameraController = cameraController;

            SelectionLine = selectionLine;
            SelectionZ = selectionZ;

            OverwriteZero = overwriteZero;
            WheelStep = wheelStep;

            CarMinLength = carMinLength;
            CarMaxLength = carMaxLength;
            NewCarDefaultDir = newCarDefaultDir;
            RequireRoadUnderCar = requireRoadUnderCar;
            CarRotateKey = carRotateKey;
            CarTurnKey = carTurnKey;
        }

        public LevelConfigViewModel Cfg => Editor?.Current.Value;

        public bool TryGetMouseCell(out Vector2Int cell)
        {
            cell = default;
            if (Editor?.Current.Value == null || GridView == null || Cam == null) return false;

            var world = Cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            int w = Editor.Current.Value.Width.Value;
            int h = Editor.Current.Value.Height.Value;
            return GridView.TryWorldToCell(world, w, h, out cell);
        }
    }
}
