using System;
using AES.Tools;
using ParkingCat.World;
using UnityEngine;


namespace ParkingCat.Game.Grid
{
    public sealed class GridBinder: ContextListenerBase
    {
        [SerializeField] GridTilemapView gridView;
        [SerializeField] UnityWorldQuery worldQuery;

        IDisposable _sub;
        
        protected override void OnContextAvailable(IBindingContext ctx)
        {
            var vm = ctx.GetValue() as GameViewModel;
            if (vm == null) return;
            
            _sub = vm.Grid01.Subscribe(grid =>
            {
                if (grid == null) return;
                gridView.Draw(grid); 
                SetupWorldQueryBounds(grid);
            });
        }
        protected override void OnContextUnavailable()
        {
            _sub?.Dispose();
        }


        void SetupWorldQueryBounds(int[,] grid)
        {
            int h = grid.GetLength(0);
            int w = grid.GetLength(1);

            float cell = EstimateCellWorldSize();

            var minCenter = gridView.CellCenterWorld(new Vector2Int(0, 0));
            var maxCenter = gridView.CellCenterWorld(new Vector2Int(w - 1, h - 1));

            float minX = minCenter.x - cell * 0.5f;
            float minY = minCenter.y - cell * 0.5f;
            float maxX = maxCenter.x + cell * 0.5f;
            float maxY = maxCenter.y + cell * 0.5f;

            worldQuery.SetBounds(new Rect(minX, minY, maxX - minX, maxY - minY)); // :contentReference[oaicite:2]{index=2}
        }

        float EstimateCellWorldSize()
        {
            var c = gridView.CellCenterWorld(Vector2Int.zero);
            var r = gridView.CellCenterWorld(Vector2Int.right);
            var u = gridView.CellCenterWorld(Vector2Int.up);

            float dx = (r - c).magnitude;
            float dy = (u - c).magnitude;
            if (dx > 1e-4f) return dx;
            if (dy > 1e-4f) return dy;
            return 1f;
        }
    }
    
}