using AES.Tools;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace ParkingCat
{
    public class DevHud : PersistentSingleton<DevHud>
    {
        [Inject]
        private ParkingCat.SceneFlow.SceneFlow _sceneFlow;

        [Header("Auth")]
        [SerializeField] private string password = "1234";
        [SerializeField] private bool allowInDevelopmentBuildOnly = true;

        [Header("Reveal (Top Invisible Tap)")]
        [SerializeField] private int revealTapCountLocked = 20;   // 잠금 상태에서 필요 탭 수
        [SerializeField] private int revealTapCountUnlocked = 1;  // 잠금 해제면 1회
        [SerializeField] private float tapTimeoutSeconds = 1.0f;  // 연속 판정 (이 시간 넘으면 리셋)

        [Header("Stage Presets")]
        [SerializeField] private int maxStage = 100;
        [SerializeField] private List<int> stagePresets = new List<int> { 1, 5, 10, 20, 30, 50 };

        private UIDocument _doc;
        private VisualElement _root;

        // Trigger area (always available)
        private VisualElement _triggerArea;

        private VisualElement _devHudRoot;
        private VisualElement _lockSection;
        private VisualElement _stageSection;

        private TextField _passwordField;
        private Button _unlockButton;
        private HelpBox _authHelpBox;

        private Label _buildLabel;
        private Label _currentStageLabel;

        private Button _reloadButton;
        private Button _prevButton;
        private Button _nextButton;

        private IntegerField _stageIndexField;
        private Button _goIndexButton;

        private DropdownField _stageDropdown;
        private Button _goDropdownButton;

        private HelpBox _stageHelpBox;
        private Button _closeButton;

        private bool _unlocked;

        private readonly List<string> _stageChoices = new();

        // 현재 스테이지는 DevHud가 알고 있어야 표시 가능(또는 외부에서 주입/동기화)
        private int _currentStage = 1;

        private CancellationTokenSource _cts;

        // tap state
        private int _tapCount;
        private float _lastTapTime;

        protected override void Awake()
        {
            base.Awake();

            if (allowInDevelopmentBuildOnly && !Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }

            _cts = new CancellationTokenSource();

            _doc = GetComponent<UIDocument>();
            _root = _doc.rootVisualElement;

            CacheElements();
            WireEvents();

            BuildStageChoices();
            RefreshCurrentStageUI();

            SetUnlocked(false);
            SetVisible(false);

            _buildLabel.text = $"Build: {Application.version} ({(Debug.isDebugBuild ? "Dev" : "Release")})";
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void CacheElements()
        {
            _triggerArea = _root.Q<VisualElement>("DevHudTriggerArea");

            _devHudRoot = _root.Q<VisualElement>("DevHudRoot");
            _lockSection = _root.Q<VisualElement>("LockSection");
            _stageSection = _root.Q<VisualElement>("StageSection");

            _passwordField = _root.Q<TextField>("PasswordField");
            _unlockButton = _root.Q<Button>("UnlockButton");
            _authHelpBox = _root.Q<HelpBox>("AuthHelpBox");

            _buildLabel = _root.Q<Label>("BuildLabel");
            _currentStageLabel = _root.Q<Label>("CurrentStageLabel");

            _reloadButton = _root.Q<Button>("ReloadButton");
            _prevButton = _root.Q<Button>("PrevStageButton");
            _nextButton = _root.Q<Button>("NextStageButton");

            _stageIndexField = _root.Q<IntegerField>("StageIndexField");
            _goIndexButton = _root.Q<Button>("GoIndexButton");

            _stageDropdown = _root.Q<DropdownField>("StageDropdown");
            _goDropdownButton = _root.Q<Button>("GoDropdownButton");

            _stageHelpBox = _root.Q<HelpBox>("StageHelpBox");
            _closeButton = _root.Q<Button>("CloseButton");
        }

        private void WireEvents()
        {
            if (_triggerArea != null)
                _triggerArea.RegisterCallback<PointerDownEvent>(OnTriggerAreaTapped);

            _unlockButton.clicked += TryUnlock;
            _passwordField.RegisterCallback<KeyDownEvent>(OnPasswordKeyDown);

            _reloadButton.clicked += ReloadCurrent;
            _prevButton.clicked += LoadPrev;
            _nextButton.clicked += LoadNext;

            _goIndexButton.clicked += () => LoadStage(_stageIndexField.value);
            _goDropdownButton.clicked += LoadFromDropdown;

            _closeButton.clicked += () => SetVisible(false);
        }

        private void OnTriggerAreaTapped(PointerDownEvent evt)
        {
            // HUD가 이미 떠있으면 무시 (원하면 토글로 바꿀 수 있음)
            if (_devHudRoot != null && _devHudRoot.visible)
                return;

            float now = UnityEngine.Time.unscaledTime;

            // 연속 탭 판정: timeout 넘으면 리셋
            if (now - _lastTapTime > tapTimeoutSeconds)
                _tapCount = 0;

            _lastTapTime = now;
            _tapCount++;

            int need = _unlocked ? revealTapCountUnlocked : revealTapCountLocked;
            if (_tapCount >= need)
            {
                _tapCount = 0;
                SetVisible(true);
            }
        }

        private void OnPasswordKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                TryUnlock();
        }

        private void TryUnlock()
        {
            var input = _passwordField.value ?? string.Empty;

            if (string.Equals(input, password, StringComparison.Ordinal))
            {
                SetUnlocked(true);
                _authHelpBox.text = "Unlocked";
                _authHelpBox.messageType = HelpBoxMessageType.Info;
                _passwordField.value = string.Empty;
            }
            else
            {
                _authHelpBox.text = "Wrong password";
                _authHelpBox.messageType = HelpBoxMessageType.Error;
                _passwordField.Focus();
                _passwordField.SelectAll();
            }
        }

        private void SetUnlocked(bool unlocked)
        {
            _unlocked = unlocked;

            if (_unlocked) _stageSection.RemoveFromClassList("hidden");
            else _stageSection.AddToClassList("hidden");
        }

        private void SetVisible(bool visible)
        {
            _devHudRoot.visible = visible;
            _devHudRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible)
            {
                RefreshCurrentStageUI();
                if (_unlocked) _stageHelpBox.text = "Ready";
            }
        }

        private void BuildStageChoices()
        {
            _stageChoices.Clear();

            foreach (var s in stagePresets)
            {
                if (s < 1) continue;
                _stageChoices.Add($"Stage {s}");
            }

            _stageDropdown.choices = _stageChoices;

            if (_stageChoices.Count > 0)
                _stageDropdown.value = _stageChoices[0];
        }

        private void RefreshCurrentStageUI()
        {
            _currentStageLabel.text = $"Stage: {_currentStage}";
            _stageIndexField.value = _currentStage;
        }

        private void ReloadCurrent()
        {
            if (!RequireUnlocked()) return;
            LoadStage(_currentStage);
        }

        private void LoadPrev()
        {
            if (!RequireUnlocked()) return;
            LoadStage(Mathf.Max(1, _currentStage - 1));
        }

        private void LoadNext()
        {
            if (!RequireUnlocked()) return;
            LoadStage(Mathf.Min(maxStage, _currentStage + 1));
        }

        private void LoadFromDropdown()
        {
            if (!RequireUnlocked()) return;

            int idx = _stageChoices.IndexOf(_stageDropdown.value);
            if (idx < 0 || idx >= stagePresets.Count)
            {
                SetStageStatus("Invalid selection", HelpBoxMessageType.Error);
                return;
            }

            LoadStage(stagePresets[idx]);
        }

        // 인게임 씬은 1개지만, StageNumber로 컨텐츠 분기하므로 SceneFlow GoInGameAsync(stageNumber)만 호출
        private void LoadStage(int stageNumber)
        {
            if (!RequireUnlocked()) return;

            if (stageNumber < 1 || stageNumber > maxStage)
            {
                SetStageStatus($"Out of range (1 ~ {maxStage})", HelpBoxMessageType.Error);
                return;
            }

            if (_sceneFlow == null)
            {
                SetStageStatus("SceneFlow not injected", HelpBoxMessageType.Error);
                return;
            }

            _currentStage = stageNumber;
            RefreshCurrentStageUI();

            SetStageStatus($"Loading Stage {stageNumber}...", HelpBoxMessageType.Info);

            _sceneFlow.GoInGameAsync(stageNumber, _cts.Token).Forget();
        }

        private bool RequireUnlocked()
        {
            if (_unlocked) return true;
            _authHelpBox.text = "Locked: enter password first";
            _authHelpBox.messageType = HelpBoxMessageType.Warning;
            return false;
        }

        private void SetStageStatus(string msg, HelpBoxMessageType type)
        {
            _stageHelpBox.text = msg;
            _stageHelpBox.messageType = type;
        }
    }
}
