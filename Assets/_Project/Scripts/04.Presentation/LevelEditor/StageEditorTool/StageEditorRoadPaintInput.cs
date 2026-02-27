using AES.Tools;
using ParkingCat.Game.Grid;
using ParkingCat.LevelEditor.StageEditorTool.Context;
using ParkingCat.LevelEditor.StageEditorTool.Tools;
using UnityEngine;
using UnityEngine.EventSystems;


namespace ParkingCat.LevelEditor.StageEditorTool
{
    public sealed class StageEditorRoadPaintInput : ContextListenerBase
    {
        [Header("Refs")]
        [SerializeField]
        Camera cam;
        [SerializeField] EditorCameraController editorCamera; // 에디터 전용 카메라
        [SerializeField] GridTilemapView gridView;

        [Header("Selection Visual")]
        [SerializeField]
        LineRenderer selectionLine;
        [SerializeField] float selectionZ = 0f;


        [Header("Select/Move")]
        [Tooltip("Hold Space to use selection tool. Otherwise paint.")]
        [SerializeField]
        bool spaceToSelect = true;

        [Tooltip("When pasting/moving, if false then 0 is treated as transparent (won't overwrite).")]
        [SerializeField]
        bool overwriteZero = true;

        [Header("Tool Hotkeys")]
        [SerializeField]
        KeyCode carToolKey = KeyCode.C;
        [SerializeField] KeyCode drawToolKey = KeyCode.D;
        [SerializeField] KeyCode selectToolKey = KeyCode.Space;

        [SerializeField] GameObject CarRoot;
        [SerializeField] KeyCode hideCar= KeyCode.H;

        [Tooltip("Rotate selected car with this key")]
        [SerializeField]
        KeyCode carRotateKey = KeyCode.R;
        [SerializeField] KeyCode carTurnKey = KeyCode.T;
        
        
        [SerializeField] KeyCode trafficLightToolKey = KeyCode.L;

        [SerializeField] int carMinLength = 2;
        [SerializeField] int carMaxLength = 5;

        [Tooltip("New car default dir")]
        [SerializeField]
        Dir4 newCarDefaultDir = Dir4.Right;

        [Tooltip("If true, require road(1) under ALL car cells to place/move/resize.")]
        [SerializeField]
        bool requireRoadUnderCar = false;

        [Header("Camera Pan (Middle Drag)")]
        [SerializeField]
        bool middleDragPan = true;
        [SerializeField] float panMultiplier = 1f;

        [Header("Editor Camera Hotkeys")]
        [SerializeField]
        KeyCode fitKey = KeyCode.F;
        [SerializeField] int wheelStep = 1;

        [SerializeField] TMPro.TMP_Text hintText;
        [SerializeField] GameObject hintRoot;
        
        [SerializeField] KeyCode oneWayToolKey = KeyCode.O;
        

        bool _isPanning;
        Vector3 _panStartWorld;
        Vector3 _camStartPos;

        LevelEditorViewModel _editor;
        LevelConfigViewModel _cfgSubscribed;

        StageEditorToolContext _ctx;

        readonly StageEditorPaintTool _paintTool = new();
        readonly StageEditorSelectionTool _selectionTool = new();
        readonly StageEditorCarTool _carTool = new();
        readonly StageEditorTrafficLightTool _trafficLightTool = new();
        readonly StageEditorOneWayTool _oneWayTool = new();

        IStageEditorTool _activeTool;

        int[,] _tmpGrid01;

        void Awake()
        {
            if (cam == null)
                cam = Camera.main;

            // editorCamera가 있으면 targetCamera를 cam으로 쓰는 게 안전
            if (editorCamera != null)
            {
                // EditorCameraController는 내부에서 targetCamera를 가지고 있으니,
                // 여기서는 단순히 cam이 null일 때만 보조로 잡음.
                if (cam == null) cam = editorCamera.GetComponent<Camera>();
            }

            if (selectionLine != null)
            {
                selectionLine.positionCount = 5;
                selectionLine.loop = false;
                selectionLine.enabled = false;
                selectionLine.useWorldSpace = true;
            }
        }

        protected override void OnContextAvailable(IBindingContext ctx)
        {
            _editor = ctx.GetValue() as LevelEditorViewModel;
            HookRoadChanged();
        }

        protected override void OnContextUnavailable()
        {
            _editor = null;
            _ctx = null;
        }

        void HookRoadChanged()
        {
            if (_editor?.Current.Value == null) return;

            _cfgSubscribed = _editor.Current.Value;
        }


        void EnsureContext()
        {
            if (_ctx != null) return;
            if (_editor?.Current.Value == null) return;

            _ctx = new StageEditorToolContext(
                editor: _editor,
                cam: cam,
                gridView: gridView,
                selectionLine: selectionLine,
                selectionZ: selectionZ,
                overwriteZero: overwriteZero,
                carMinLength: carMinLength,
                carMaxLength: carMaxLength,
                newCarDefaultDir: newCarDefaultDir,
                requireRoadUnderCar: requireRoadUnderCar,
                wheelStep: wheelStep,
                carRotateKey: carRotateKey,
                carTurnKey: carTurnKey);

            _ctx.RefreshPreview = () => _editor.RefreshPreviewCommand.Execute();
            _ctx.PushUndo = () => _editor.PushUndo();
            _ctx.DrawGridFromRoad01 = DrawGridFromRoad01;
            _ctx.IsCtrlDown = IsCtrlDown;
            _ctx.IsAltDown = IsAltDown;

            _ctx.SetHintText = s =>
            {
                if (hintText) hintText.text = s;
            };

            _ctx.SetHintVisible = on =>
            {
                if (hintRoot) hintRoot.SetActive(on);
                else if (hintText) hintText.gameObject.SetActive(on);
            };

            // 최초 진입 힌트도 한번 세팅
            SwitchTool(_paintTool);
        }

        void SwitchTool(IStageEditorTool next)
        {
            if (_activeTool == next) return;
            _activeTool?.OnExit(_ctx);
            _activeTool = next;
            _activeTool?.OnEnter(_ctx);

            // 힌트 갱신
            _ctx?.SetHintVisible?.Invoke(true);
            _ctx?.SetHintText?.Invoke(GetHintForTool(_activeTool));
        }

        string GetHintForTool(IStageEditorTool tool)
        {
            if (tool == _carTool)
                return
                    @"[차 배치 모드]
좌클릭: 차량 배치 / 선택
우클릭: 차량 삭제 
R: 회전
T: 턴 방향 변경 (Shift: 반대)";

            if (tool == _selectionTool)
                return
                    @"[선택 모드]
드래그: 영역 선택
선택 영역 드래그: 이동 (Ctrl: 복사)
Delete: 삭제
우클릭: 선택 해제";

            if (tool == _trafficLightTool)
                return
                    @"[신호등 배치 모드]
좌클릭: 신호등 배치 / 선택
우클릭: 삭제
R: 판정선 방향 회전@";
            
            if (tool == _oneWayTool)
                return
                    @"[일방통행 툴]
좌클릭: 클릭한 차선 1개 토글(삭제/생성)
R: 허용 방향 회전";
            
            // paint
            return
                @"[도로 배치 모드]
좌클릭: 길 배치
우클릭: 지우기
Shift+클릭: 영역 채우기";
            
            
        }

        void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (_editor?.Current.Value == null) return;
            if (gridView == null || cam == null) return;

            EnsureContext();

            bool isEditing = _editor.Mode.Value == EditorStageMode.Editing;
            _ctx?.SetHintVisible?.Invoke(isEditing);
            if (!isEditing) return;

            if (!ReferenceEquals(_cfgSubscribed, _editor.Current.Value))
                HookRoadChanged();

            HandleUndoRedoHotkeys();
            HandleSaveHotkey();

            // F : Fit (Editor only)
            if (UnityEngine.Input.GetKeyDown(fitKey))
            {
                if (editorCamera != null) editorCamera.FitToBoard(true);
            }

            // Tool switching (persistent)
            if (UnityEngine.Input.GetKeyDown(KeyCode.C))        // Car tool
            {
                CarRoot?.SetActive(true);
                SwitchTool(_carTool);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.D))   // Draw tool
            {
                SwitchTool(_paintTool);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) // Selection tool
            {
                SwitchTool(_selectionTool);
            }
            else if (UnityEngine.Input.GetKeyDown(trafficLightToolKey))
            {
                SwitchTool(_trafficLightTool);
            }
            else if (UnityEngine.Input.GetKeyDown(oneWayToolKey))
            {
                SwitchTool(_oneWayTool);
            }
            else if (UnityEngine.Input.GetKeyDown(hideCar) && CarRoot != null)
            {
                var active = CarRoot.activeSelf;
                CarRoot.SetActive(!active);
            }

            // 카메라 입력은 툴과 무관하게 항상 동작
            HandleMiddleDragPan();
            HandleWheelZoom();

            // Active tool tick
            _activeTool.Tick(_ctx);
        }

        void LateUpdate()
        {
            if (_ctx == null) return;

            if (_activeTool == _selectionTool) { _selectionTool.UpdateSelectionLine(_ctx); }
            else if (_ctx.SelectionLine != null) { _ctx.SelectionLine.enabled = false; }
        }

        void HandleWheelZoom()
        {
            float wheel = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f) return;

            // EditorCameraController가 있으면 거기로 위임
            if (editorCamera != null)
            {
                editorCamera.ZoomAtScreenPoint(wheel, UnityEngine.Input.mousePosition);
                return;
            }

            // Fallback: cam 직접 줌(커서 중심) + clamp 없음
            var mouse = UnityEngine.Input.mousePosition;

            Vector3 before = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, cam.nearClipPlane));

            float zoomFactor = 0.9f;

            cam.orthographicSize = wheel > 0
                ? cam.orthographicSize * zoomFactor
                : cam.orthographicSize / zoomFactor;

            Vector3 after = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, cam.nearClipPlane));
            cam.transform.position += (before - after);
        }

        void HandleMiddleDragPan()
        {
            if (!middleDragPan || cam == null) return;

            // EditorCameraController가 있으면 거기로 위임(권장)
            if (editorCamera != null)
            {
                if (UnityEngine.Input.GetMouseButtonDown(2)) editorCamera.BeginPan(UnityEngine.Input.mousePosition);
                if (UnityEngine.Input.GetMouseButton(2)) editorCamera.PanTo(UnityEngine.Input.mousePosition);
                if (UnityEngine.Input.GetMouseButtonUp(2)) editorCamera.EndPan();
                return;
            }

            // Fallback: cam 직접 패닝
            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                _isPanning = true;
                var m = UnityEngine.Input.mousePosition;
                _panStartWorld = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, cam.nearClipPlane));
                _camStartPos = cam.transform.position;
            }

            if (_isPanning && UnityEngine.Input.GetMouseButton(2))
            {
                var m = UnityEngine.Input.mousePosition;
                var nowWorld = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, cam.nearClipPlane));

                Vector3 delta = _panStartWorld - nowWorld;
                cam.transform.position = _camStartPos + delta * panMultiplier;
            }

            if (UnityEngine.Input.GetMouseButtonUp(2)) { _isPanning = false; }
        }

        void HandleUndoRedoHotkeys()
        {
            // Ctrl+Z : Undo,  Ctrl+Shift+Z : Redo
            if (IsCtrlDown() && UnityEngine.Input.GetKeyDown(KeyCode.Z))
            {
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);

                if (shift) _editor?.RedoCommand?.Execute();
                else _editor?.UndoCommand?.Execute();

                return;
            }

            // Ctrl+Y : Redo
            if (IsCtrlDown() && UnityEngine.Input.GetKeyDown(KeyCode.Y)) { _editor?.RedoCommand?.Execute(); }
        }

        static bool IsCtrlDown()
        {
            bool ctrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
            bool cmd = UnityEngine.Input.GetKey(KeyCode.LeftCommand) || UnityEngine.Input.GetKey(KeyCode.RightCommand);
            return ctrl || cmd;
        }

        static bool IsAltDown()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
        }

        void DrawGridFromRoad01(int[] road01, int w, int h)
        {
            if (_tmpGrid01 == null || _tmpGrid01.GetLength(0) != h || _tmpGrid01.GetLength(1) != w)
                _tmpGrid01 = new int[h, w];

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    _tmpGrid01[y, x] = road01[y * w + x];

            gridView.Draw(_tmpGrid01);
        }

        void HandleSaveHotkey()
        {
            if (!IsCtrlDown()) return;
            if (!UnityEngine.Input.GetKeyDown(KeyCode.S)) return;

            // 편집 모드 + 커맨드 가능 여부 체크
            if (_editor?.SaveAsJsonCommand != null &&
                _editor.SaveAsJsonCommand.CanExecute())
            {
                _editor.SaveAsJsonCommand.Execute();
            }
        }
    }
}