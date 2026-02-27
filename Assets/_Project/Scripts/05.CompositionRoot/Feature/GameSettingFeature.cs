using AES.Tools;
using AES.Tools.VContainer.Bootstrap.Framework;
using Cysharp.Threading.Tasks;
using JSAM;
using ParkingCat.Lobby.Popups;
using ParkingCat.Repository;
using ParkingCat.Repository.Settings;
using ParkingCat.UseCase.UserSettings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using VContainer;
using VContainer.Unity;

namespace ParkingCat.Feature
{
    [CreateAssetMenu(menuName="Bootstrap/Features/Game Setting", fileName="GameSettingFeature")]
    public sealed class GameSettingFeature : AppFeatureSO
    {
        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.Register<SettingViewModel>(Lifetime.Singleton);
            // Repository
            builder.Register<SettingsRepository>(Lifetime.Singleton)
                .As<ISettingsRepository>();

            // UseCases
            builder.Register<GetUserSettingUseCase>(Lifetime.Singleton);
            builder.Register<SetUserSettingUseCase>(Lifetime.Singleton);
        }

        public override async UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
        {
            var settings = await rootScope.Container
                .Resolve<GetUserSettingUseCase>()
                .Execute();

            // Vibration
            Haptic.Initialize();
            Haptic.Using = settings.Vibration;
            
            AudioManager.MusicMuted = settings.IsBgmMute;
            AudioManager.SoundMuted = settings.IsSfxMute;

            // Language (Unity Localization)
            await ApplyLanguageAsync(settings.Language);
        }

        async static UniTask ApplyLanguageAsync(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return;

            // Localization 초기화 대기
            await UniTask.WaitUntil(() => LocalizationSettings.InitializationOperation.IsDone);

            // localeCode 예: "ko-KR", "en", "en-US" 등
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);

            // 정확히 못 찾으면 Code만 비교해서 fallback (예: "en-US" 저장, 프로젝트엔 "en"만 있는 경우)
            if (locale == null)
            {
                foreach (var l in LocalizationSettings.AvailableLocales.Locales)
                {
                    if (string.Equals(l.Identifier.Code, localeCode, System.StringComparison.OrdinalIgnoreCase))
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
