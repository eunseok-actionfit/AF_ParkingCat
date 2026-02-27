using System;
using AES.Tools;
using ParkingCat.Game;
using ParkingCat.Game.Grid;
using UnityEngine;


namespace ParkingCat.LevelEditor
{
    /// <summary>
    /// GameViewModel.Grid01(int[,])의 크기를 기준으로 사각 SpriteMask를 자동으로 맞춘다.
    /// - Tilemap에 의존하지 않고, 모델(Grid01)만으로 마스크 영역을 계산한다.
    /// - GridTilemapView의 CellCenterWorld 변환을 사용해 월드 bounds를 얻는다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteMask))]
    public sealed class Grid01TargetMask : ContextListenerBase
    {
        [Header("Target")]
        [SerializeField]
        GridTilemapView gridView;

        [Header("Options")]
        [Tooltip("gridView의 회전/스케일을 그대로 따르려면 true(대부분 true 권장)")]
        [SerializeField]
        bool followGridViewTransform = true;

        [Tooltip("마스크 여백(월드 유닛)")]
        [SerializeField]
        Vector2 paddingWorld = Vector2.zero;

        [Tooltip("grid가 비었으면 아무것도 안 보이게끔 (mask enabled off)")]
        [SerializeField]
        bool disableWhenEmpty = true;

        IDisposable _sub;
        SpriteMask _mask;
        Sprite _unitSprite; // 1x1 white sprite (PPU=1)

        int[,] _lastGrid;

        void Reset()
        {
            _mask = GetComponent<SpriteMask>();
            EnsureUnitSprite();
            Apply(_lastGrid);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _mask = GetComponent<SpriteMask>();
            EnsureUnitSprite();
            Apply(_lastGrid);
        }
#endif

        void LateUpdate()
        {
            // 에디터에서 GridView transform 변경 등에 따라가야 하므로 ExecuteAlways에서는 매 프레임 반영.
            // 런타임 비용이 걱정되면 dirty 플래그로 전환.
            Apply(_lastGrid);
        }

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            var vm = ctx.GetValue() as GameViewModel;
            if (vm == null) return;

            _sub = vm.Grid01.Subscribe(grid =>
            {
                _lastGrid = grid;
                Apply(grid);
            });
        }

        protected override void OnContextUnavailable()
        {
            _sub?.Dispose();
            _sub = null;
        }

        public void Apply(int[,] grid)
        {
            if (_mask == null) _mask = GetComponent<SpriteMask>();
            if (_mask == null) return;
            if (gridView == null) return;

            if (grid == null || grid.Length == 0)
            {
                if (disableWhenEmpty) _mask.enabled = false;
                return;
            }

            int h = grid.GetLength(0);
            int w = grid.GetLength(1);
            if (w <= 0 || h <= 0)
            {
                if (disableWhenEmpty) _mask.enabled = false;
                return;
            }

            _mask.enabled = true;

            // 마스크는 1x1 스프라이트를 월드 크기로 스케일
            EnsureUnitSprite();
            _mask.sprite = _unitSprite;

            float cell = EstimateCellWorldSize();

            // (0,0) ~ (w-1,h-1) 셀 중심을 이용해 전체 bounds 산출
            var minCenter = gridView.CellCenterWorld(new Vector2Int(0, 0));
            var maxCenter = gridView.CellCenterWorld(new Vector2Int(w - 1, h - 1));

            float minX = minCenter.x - cell * 0.5f;
            float minY = minCenter.y - cell * 0.5f;
            float maxX = maxCenter.x + cell * 0.5f;
            float maxY = maxCenter.y + cell * 0.5f;

            float worldW = Mathf.Abs(maxX - minX) + paddingWorld.x * 2f;
            float worldH = Mathf.Abs(maxY - minY) + paddingWorld.y * 2f;

            Vector3 worldCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);

            if (followGridViewTransform)
            {
                // gridView transform을 따르되, 크기는 world unit 기준으로 맞춤
                var t = gridView.transform;
                transform.SetPositionAndRotation(t.position, t.rotation);

                // local로 변환해서 중심 맞추기
                Vector3 localCenter = t.InverseTransformPoint(worldCenter);
                transform.localPosition = localCenter;

                // worldW/H를 local scale로 보정
                Vector3 lossy = t.lossyScale;
                float invX = Mathf.Abs(lossy.x) > 0.0001f ? 1f / Mathf.Abs(lossy.x) : 1f;
                float invY = Mathf.Abs(lossy.y) > 0.0001f ? 1f / Mathf.Abs(lossy.y) : 1f;

                transform.localScale = new Vector3(worldW * invX, worldH * invY, 1f);
            }
            else
            {
                transform.position = worldCenter;
                transform.rotation = Quaternion.identity;
                transform.localScale = new Vector3(worldW, worldH, 1f);
            }
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

        void EnsureUnitSprite()
        {
            if (_unitSprite != null) return;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);

            _unitSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), pixelsPerUnit: 1f);
            _unitSprite.name = "UnitWhiteSprite_Runtime";
        }
    }
}
