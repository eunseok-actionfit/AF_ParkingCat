using System;
using System.Collections.Generic;
using AES.Tools;
using ParkingCat.Game.Grid;
using UnityEngine;


namespace ParkingCat.LevelEditor.StageEditorTool
{
    public sealed class StageEditorGridOverlay : ContextListenerBase
    {
        [Header("Refs")]
        [SerializeField]
        GridTilemapView gridView;

        [Header("Line Prefabs (템플릿)")]
        [SerializeField]
        LineRenderer minorLinePrefab;
        [SerializeField] LineRenderer majorLinePrefab;

        [Header("Sizing")]
        [SerializeField]
        float cellSize = 1f;
        [SerializeField] Vector3 origin = Vector3.zero;

        [Header("Major Grid")]
        [SerializeField]
        int majorEvery = 5;

        [Header("Visibility")]
        [SerializeField]
        bool showGrid = true;
        [SerializeField] bool showMajor = true;

        [Header("Line Widths")]
        [SerializeField]
        float minorWidth = 0.02f;
        [SerializeField] float majorWidth = 0.04f;

        [Header("Depth")]
        [SerializeField]
        float z = 0f;

        LevelEditorViewModel _editor;
        LevelConfigViewModel _cfg;
        IDisposable _wSub;
        IDisposable _hSub;
        IDisposable _currentSub;

        // 풀
        readonly List<LineRenderer> _minorPool = new();
        readonly List<LineRenderer> _majorPool = new();

        void Awake()
        {
            SetupPrefab(minorLinePrefab, minorWidth);
            SetupPrefab(majorLinePrefab, majorWidth);
            ApplyVisibility();
        }

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            _editor = ctx.GetValue() as LevelEditorViewModel;
            Hook();
            Rebuild();
        }

        protected override void OnContextUnavailable()
        {
            Unhook();
            _editor = null;
        }

        void Update()
        {
            if (_editor == null) return;
            if (_editor.Mode.Value != EditorStageMode.Editing) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.G))
            {
                if (IsShiftDown()) ToggleMajor();
                else ToggleGrid();
            }
        
        }

        public void ToggleGrid()
        {
            showGrid = !showGrid;
            ApplyVisibility();
        }

        public void ToggleMajor()
        {
            showMajor = !showMajor;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            bool minorOn = showGrid;
            bool majorOn = showGrid && showMajor;

            SetPoolVisible(_minorPool, minorOn);
            SetPoolVisible(_majorPool, majorOn);

            // 템플릿은 씬에서 보이면 헷갈리니 꺼두는 걸 권장
            if (minorLinePrefab) minorLinePrefab.enabled = false;
            if (majorLinePrefab) majorLinePrefab.enabled = false;
        }

        void Hook()
        {
            Unhook();
            if (_editor == null) return;

            _currentSub = _editor.Current.Subscribe(_ =>
            {
                HookCurrent();
                Rebuild();
            });

            HookCurrent();
        }

        void HookCurrent()
        {
            _wSub?.Dispose();
            _hSub?.Dispose();
            _wSub = _hSub = null;

            _cfg = _editor.Current.Value;
            if (_cfg == null) return;

            _wSub = _cfg.Width.Subscribe(_ => Rebuild());
            _hSub = _cfg.Height.Subscribe(_ => Rebuild());
        }

        void Unhook()
        {
            _wSub?.Dispose();
            _hSub?.Dispose();
            _currentSub?.Dispose();
            _wSub = _hSub = _currentSub = null;
            _cfg = null;
        }

        public void Rebuild()
        {
            if (_cfg == null) return;

            int w = Mathf.Max(1, _cfg.Width.Value);
            int h = Mathf.Max(1, _cfg.Height.Value);

            if (!showGrid)
            {
                ClearPools();      // 선택: 숨기기만 할거면 ClearPools() 대신 ApplyVisibility()만 해도 됨
                ApplyVisibility();
                return;
            }

            BuildMinor(w, h);
            BuildMajor(w, h);

            ApplyVisibility();
        }

        void BuildMinor(int w, int h)
        {
            int needed = (w + 1) + (h + 1);
            EnsurePool(_minorPool, minorLinePrefab, needed);

            // 핵심: 일단 전부 끄고 시작
            DisableUnused(_minorPool, 0);

            int idx = 0;

            for (int x = 0; x <= w; x++)
                SetLine(_minorPool[idx++], WorldPos(x, 0), WorldPos(x, h), minorWidth);

            for (int y = 0; y <= h; y++)
                SetLine(_minorPool[idx++], WorldPos(0, y), WorldPos(w, y), minorWidth);

            // optional: 여기서 다시 DisableUnused 필요 없음(이미 0으로 끄고 사용분만 켰으니)
            // DisableUnused(_minorPool, idx);
        }

        void BuildMajor(int w, int h)
        {
            EnsurePool(_majorPool, majorLinePrefab, /*대충 최대 needed 계산*/ (w/Mathf.Max(1,majorEvery)+1) + (h/Mathf.Max(1,majorEvery)+1));

            // 핵심: 일단 전부 끄고 시작
            DisableUnused(_majorPool, 0);

            if (!showMajor) return;

            int every = Mathf.Max(1, majorEvery);
            int idx = 0;

            for (int x = 0; x <= w; x += every)
                SetLine(_majorPool[idx++], WorldPos(x, 0), WorldPos(x, h), majorWidth);

            for (int y = 0; y <= h; y += every)
                SetLine(_majorPool[idx++], WorldPos(0, y), WorldPos(w, y), majorWidth);
        }


        Vector3 WorldPos(int cellX, int cellY)
        {
            float x = origin.x + cellX * cellSize;
            float y = origin.y + cellY * cellSize;
            return new Vector3(x, y, z);
        }

        static void SetupPrefab(LineRenderer lr, float width)
        {
            if (!lr) return;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.positionCount = 0;
            lr.widthMultiplier = width;
        }

        static void EnsurePool(List<LineRenderer> pool, LineRenderer prefab, int needed)
        {
            if (!prefab) return;

            while (pool.Count < needed)
            {
                var inst = Instantiate(prefab, prefab.transform.parent);
                inst.gameObject.name = prefab.gameObject.name + "_inst";
                inst.enabled = true;
                pool.Add(inst);
            }
        }

        static void SetLine(LineRenderer lr, Vector3 a, Vector3 b, float width)
        {
            lr.widthMultiplier = width;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.enabled = true;
        }

        static void DisableUnused(List<LineRenderer> pool, int used)
        {
            for (int i = used; i < pool.Count; i++)
            {
                if (pool[i]) pool[i].enabled = false;
            }
        }

        static void SetPoolVisible(List<LineRenderer> pool, bool on)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (!pool[i]) continue;

                pool[i].enabled = on;
            }
        }


        void ClearPools()
        {
            DisableUnused(_minorPool, 0);
            DisableUnused(_majorPool, 0);
        }

        static bool IsShiftDown()
            => UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
    }
}
