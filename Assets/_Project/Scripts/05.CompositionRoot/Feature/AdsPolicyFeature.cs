using System;
using AES.Tools.VContainer;
using AES.Tools.VContainer.Bootstrap.Framework;
using Cysharp.Threading.Tasks;
using ParkingCat;
using ParkingCat.Repository;
using VContainer;
using UnityEngine;
using VContainer.Unity;


public sealed class AdsPolicyFeature : AppFeatureSO
{
    [Header("Level Gates")]
    [SerializeField] private int bannerMinLevel = 20;
    [SerializeField] private int interstitialMinLevel = 20;
    [SerializeField] private int rewardedMinLevel = 15;
    [SerializeField] private int appOpenMinLevel = 20;

    [Header("Cooldown (seconds)")]
    [SerializeField] private int interstitialClearCooldown = 120;
    [SerializeField] private int interstitialFailCooldown  = 240;
    [SerializeField] private int appOpenResumeCooldown     = 180;

    private DateTime _lastClearInterstitialUtc;
    private DateTime _lastFailInterstitialUtc;
    private DateTime _lastResumeAppOpenUtc;
    private DateTime firstLaunchUtc;
    private const string FirstLaunchUtcKey = "first_launch_utc";

    
    
    public override async UniTask Initialize(
        LifetimeScope rootScope,
        FeatureContext ctx)
    {
        var adsService = rootScope.Container.Resolve<IAdsService>() as AdsService;
        if (adsService == null)
            return;
        
        if (!PlayerPrefs.HasKey(FirstLaunchUtcKey))
        {
            firstLaunchUtc = DateTime.UtcNow;
            PlayerPrefs.SetString(
                FirstLaunchUtcKey,
                firstLaunchUtc.ToString("O") // ISO 8601
            );
            PlayerPrefs.Save();
        }
        else
        {
            firstLaunchUtc = DateTime.Parse(
                PlayerPrefs.GetString(FirstLaunchUtcKey),
                null,
                System.Globalization.DateTimeStyles.RoundtripKind
            );
        }
        

        var progressRepo = rootScope.Container.Resolve<IPlayerProgressRepository>();
        await progressRepo.LoadAsync();
        

        // =========================================================
        // Banner Gate
        // =========================================================
        adsService.CanShowBannerGate = () =>
        {
            var p = progressRepo.Current;
            if (p == null) return false;

            // Lv.20 이상 + 인게임에서만 (로비 제외는 호출부에서 제어)
            return p.LastClearedLevel >= bannerMinLevel;
        };

        // =========================================================
        // Interstitial Gate
        // =========================================================
        adsService.CanShowInterstitialGate = reason =>
        {
            var p = progressRepo.Current;
            if (p == null) return false;
            if (p.LastClearedLevel < interstitialMinLevel)
                return false;

            switch (reason)
            {
                case AdReason.StageClear:
                    return Cooldown(
                        ref _lastClearInterstitialUtc,
                        interstitialClearCooldown);

                case AdReason.StageFail:
                    return Cooldown(
                        ref _lastFailInterstitialUtc,
                        interstitialFailCooldown);

                default:
                    // 다른 이유에서는 노출 금지
                    return false;
            }
        };

        // =========================================================
        // Rewarded Gate
        // =========================================================
        adsService.CanShowRewardedGate = reason =>
        {
            var p = progressRepo.Current;
            if (p == null) return false;

            // 버튼 클릭형 → 쿨타임 없음
            return p.LastClearedLevel >= rewardedMinLevel;
        };

        // =========================================================
        // AppOpen Gate
        // =========================================================
        adsService.CanShowAppOpenGate = reason =>
        {
            var p = progressRepo.Current;
            if (p == null || p.LastClearedLevel < appOpenMinLevel)
                return false;

            // 최초 실행 후 1일 제한
            if (reason == AdReason.AppLaunch &&
                (DateTime.UtcNow - firstLaunchUtc).TotalHours < 24)
                return false;

            if (reason == AdReason.Resume)
                return Cooldown(ref _lastResumeAppOpenUtc, appOpenResumeCooldown);

            return true;
        };
    }

    // =========================================================
    // Cooldown Helper
    // =========================================================
    private bool Cooldown(ref DateTime lastUtc, int seconds)
    {
        if (seconds <= 0)
            return true;

        if (lastUtc != default &&
            (DateTime.UtcNow - lastUtc).TotalSeconds < seconds)
            return false;

        lastUtc = DateTime.UtcNow;
        return true;
    }
}
