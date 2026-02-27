using System;
using System.Threading;
using AES.Tools;
using Cysharp.Threading.Tasks;
using JSAM;
using ParkingCat.Entities;
using ParkingCat.UseCase.UserSettings;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ParkingCat.Lobby.Popups
{
    public sealed class SettingViewModel
    {
        public ObservableProperty<bool> BgmMuted { get; } = new();
        public ObservableProperty<bool> SfxMuted { get; } = new();
        public ObservableProperty<bool> Vibration { get; } = new();
        public ObservableProperty<string> Language { get; } = new("ko-KR");

        
        public Command ToggleBgmMuteCommand { get; }
        public Command ToggleSfxMuteCommand { get; }
        public Command ToggleVibrationCommand { get; }
        public Command<string> SetLanguageCommand { get; }
        
        public AsyncCommand RefreshCommand { get; }
        public AsyncCommand SaveCommand { get; }

        readonly GetUserSettingUseCase _getUserSettingUseCase;
        readonly SetUserSettingUseCase _setUserSettingUseCase;

        UserSettings _settings;
        CancellationTokenSource _debounceCts;

        public SettingViewModel(
            GetUserSettingUseCase getUserSettingUseCase,
            SetUserSettingUseCase setUserSettingUseCase)
        {
            _getUserSettingUseCase = getUserSettingUseCase;
            _setUserSettingUseCase = setUserSettingUseCase;
            
            ToggleBgmMuteCommand = new Command(() => BgmMuted.Value = !BgmMuted.Value);
            ToggleSfxMuteCommand = new Command(() => SfxMuted.Value = !SfxMuted.Value);
            ToggleVibrationCommand = new Command(() =>
            {
                Vibration.Value = !Vibration.Value;
                if(Vibration.Value) Haptic.Weak();
            });
            SetLanguageCommand = new Command<string>(code => Language.Value = code);

            RefreshCommand = new AsyncCommand(_ => RefreshAsync());
            SaveCommand = new AsyncCommand(_ => SaveAsync());

            // 값 변경 시 자동 저장(디바운스)
            BgmMuted.OnValueChanged += _ =>
            {
                AudioManager.MusicMuted = BgmMuted.Value;
                ScheduleSave();
            };
            SfxMuted.OnValueChanged += mute =>
            {
                if (mute)
                    AudioManager.StopAllSounds();
                AudioManager.SoundMuted = SfxMuted.Value;
                ScheduleSave();
            };
            Vibration.OnValueChanged += _ => ScheduleSave();
            Language.OnValueChanged += _ => ScheduleSave();

            Initialize().Forget();
        }

        async UniTaskVoid Initialize()
        {
            await RefreshAsync();

            // 즉시 사이드이펙트 적용(현재 로드된 값 기준)
            ApplyVibration();
            await ApplyLanguageAsync(Language.Value);
        }

        public async UniTask RefreshAsync()
        {
            _settings = await _getUserSettingUseCase.Execute();

            // UI 바인딩 값 세팅
            BgmMuted.Value = _settings.IsBgmMute;
            SfxMuted.Value = _settings.IsSfxMute;
            Vibration.Value = _settings.Vibration;
            Language.Value = _settings.Language;
        }

        public async UniTask SaveAsync()
        {
            if (_settings == null)
                _settings = await _getUserSettingUseCase.Execute();

            // Entity 업데이트 (Clamp 포함)
            _settings.SetBgmMute(BgmMuted.Value);
            _settings.SetSfxMute(SfxMuted.Value);
            _settings.SetVibration(Vibration.Value);
            _settings.SetLanguage(Language.Value);

            await _setUserSettingUseCase.Execute(_settings);

            // 저장 후 사이드이펙트 반영
            ApplyVibration();
            await ApplyLanguageAsync(Language.Value);
        }

        void ScheduleSave()
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();

            DebouncedSaveAsync(_debounceCts.Token).Forget();
        }

        async UniTaskVoid DebouncedSaveAsync(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(250), cancellationToken: ct);
                await SaveAsync();
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        void ApplyVibration()
        {
            Haptic.Using = Vibration.Value;
        }

        async static UniTask ApplyLanguageAsync(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return;

            // Unity Localization 초기화 대기
            await LocalizationSettings.InitializationOperation.Task;

            // "ko-KR" 같은 코드로 로케일 검색
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);

            // 정확히 못 찾으면 Identifier.Code 비교로 fallback
            if (locale == null)
            {
                foreach (var l in LocalizationSettings.AvailableLocales.Locales)
                {
                    if (string.Equals(l.Identifier.Code, localeCode, StringComparison.OrdinalIgnoreCase))
                    {
                        locale = l;
                        break;
                    }
                }
            }

            if (locale != null && LocalizationSettings.SelectedLocale != locale)
                LocalizationSettings.SelectedLocale = locale;
        }
    }
}
