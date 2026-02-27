using AES.Tools;
using UnityEngine;


namespace ParkingCat.Lobby.Shop
{
    public sealed class ShopItemViewModel
    {
        public enum RewardIconKind
        {
            Coin,
            UnlimitedLivesMin,
            DoubleRewardMin,
            Boosters,
            RemoveAds,
            Unknown
        }

        public sealed class RewardPreviewVM
        {
            public RewardIconKind Kind;
            public int Amount;
            public bool ShowAmount;

            // 데이터에서 온 대표 여부(필요하면 UI에서 뱃지/정렬 등에 사용)
            public bool IsMain;
        }

        public string ProductKey { get; }

        public ObservableProperty<string> TitleKey { get; } = new("");
        public ObservableProperty<string> PriceText { get; } = new("");

        public ObservableList<RewardPreviewVM> RewardPreviews { get; } = new();

        // View 바인딩용 결과물
        public ObservableProperty<Sprite> BackgroundSprite { get; } = new();

        // 대표(메인) 보상 아이콘: IsMain 기준으로 고정 저장
        public ObservableProperty<Sprite> MainRewardSprite { get; } = new();
        public ObservableProperty<int> MainRewardAmount { get; } = new();
        public ObservableProperty<bool> HasMainReward { get; } = new();

        public ObservableProperty<Sprite> CoinSprite { get; } = new();
        public ObservableProperty<int> CoinAmount { get; } = new();
        public ObservableProperty<bool> HasCoin { get; } = new();

        public ObservableProperty<Sprite> UnlimitedLivesSprite { get; } = new();
        public ObservableProperty<int> UnlimitedLivesMin { get; } = new();
        public ObservableProperty<bool> HasUnlimitedLives { get; } = new();
        
        
        public ObservableProperty<Sprite> DoubleRewardSprite { get; } = new();
        public ObservableProperty<int> DoubleRewardMin { get; } = new();
        public ObservableProperty<bool> HasDoubleReward { get; } = new();
        
        public ObservableProperty<bool> HasDoubleRewardOrUnlimitedLives { get; } = new();

        public ObservableProperty<bool> HasBoosters { get; } = new();
        public ObservableProperty<int> BoosterAmount { get; } = new();

        public ObservableProperty<Sprite> RemoveAdsSprite { get; } = new();
        public ObservableProperty<bool> HasRemoveAds { get; } = new();

        public AsyncCommand BuyCommand { get; }

        public ShopItemViewModel(string productKey)
        {
            ProductKey = productKey;

            BuyCommand = new AsyncCommand(
                executeAsync:async _ => {
                    await AES.Tools.IAP.PurchaseByProductKeyAsync(ProductKey);
                },
                canExecute: _ => AES.Tools.IAP.IsBound && AES.Tools.IAP.IsReady
            );
        }

        public void RebuildVisuals(ShopIconTable icons, ShopThemeTable themes, string bgKey = null)
        {
            BackgroundSprite.Value = null;

            // 대표 아이콘 초기화
            HasMainReward.Value = false;
            MainRewardSprite.Value = null;
            MainRewardAmount.Value = 0;

            CoinAmount.Value = 0; HasCoin.Value = false; CoinSprite.Value = null;
            UnlimitedLivesMin.Value = 0; HasUnlimitedLives.Value = false; UnlimitedLivesSprite.Value = null;
            DoubleRewardMin.Value = 0; HasDoubleReward.Value = false; DoubleRewardSprite.Value = null;

            HasBoosters.Value = false;
            BoosterAmount.Value = 0;

            HasRemoveAds.Value = false;
            RemoveAdsSprite.Value = null;

            int coin = 0, lives = 0, doubleReward =0, boosters = 0;
            bool removeAds = false;

            // 대표(메인) row 선정(RewardPreviews에서 IsMain==true 우선)
            RewardPreviewVM main = null;

            for (int i = 0; i < RewardPreviews.Count; i++)
            {
                var r = RewardPreviews[i];
                if (r == null) continue;

                if (main == null && r.IsMain)
                    main = r;

                switch (r.Kind)
                {
                    case RewardIconKind.Coin: coin += r.Amount; break;
                    case RewardIconKind.UnlimitedLivesMin: lives += r.Amount; break;
                    case RewardIconKind.DoubleRewardMin: doubleReward += r.Amount; break;

                    case RewardIconKind.Boosters:
                        // 중복 방지: max로 정규화
                        boosters = Mathf.Max(boosters, r.Amount);
                        break;

                    case RewardIconKind.RemoveAds:
                        removeAds = true;
                        break;
                }
            }

            // fallback: IsMain이 없다면 "첫 유효 보상"을 메인으로
            if (main == null)
            {
                for (int i = 0; i < RewardPreviews.Count; i++)
                {
                    var r = RewardPreviews[i];
                    if (r == null) continue;
                    if (r.Kind == RewardIconKind.Unknown) continue;
                    main = r;
                    break;
                }
            }

            // 대표(메인) 아이콘 스프라이트 확정 저장
            if (main != null)
            {
                HasMainReward.Value = true;
                MainRewardAmount.Value = main.Amount;
                MainRewardSprite.Value = ResolveMainRewardSprite(main, icons);
            }

            if (coin > 0)
            {
                HasCoin.Value = true;
                CoinAmount.Value = coin;
                CoinSprite.Value = icons != null ? icons.GetCoinSprite(coin) : null;
            }

            if (lives > 0)
            {
                HasUnlimitedLives.Value = true;
                UnlimitedLivesMin.Value = lives;
                UnlimitedLivesSprite.Value = icons != null ? icons.heart : null;
            }
            
            if (doubleReward > 0)
            {
                // HasDoubleReward.Value = true;
                // DoubleRewardMin.Value = doubleReward;
                // DoubleRewardSprite.Value = icons != null ? icons.doubleReward : null;
            }
            
            HasDoubleRewardOrUnlimitedLives.Value = HasDoubleReward.Value || HasUnlimitedLives.Value;

            if (boosters > 0)
            {
                HasBoosters.Value = true;
                BoosterAmount.Value = boosters;
                // 아이콘 3종은 프리팹 고정 스프라이트 권장
            }

            HasRemoveAds.Value = removeAds;
            if (removeAds)
                RemoveAdsSprite.Value = icons != null ? icons.removeAds : null;

            if (themes != null)
            {
                var key = string.IsNullOrWhiteSpace(bgKey) ? ProductKey : bgKey;
                BackgroundSprite.Value = themes.GetBundleBg(key);
            }
        }

        static Sprite ResolveMainRewardSprite(RewardPreviewVM main, ShopIconTable icons)
        {
            if (icons == null || main == null) return null;

            switch (main.Kind)
            {
                case RewardIconKind.Coin:
                    return icons.GetCoinSprite(main.Amount);
                case RewardIconKind.UnlimitedLivesMin:
                    return icons.heart;
                case RewardIconKind.RemoveAds:
                    return icons.removeAds;
                case RewardIconKind.Boosters:
                    // 대표 부스터 아이콘 1개 고정(원하면 boosterUfo로 바꿔도 됨)
                    return icons.boosterHint;
                case RewardIconKind.DoubleRewardMin:
                    return icons.doubleReward;
                case RewardIconKind.Unknown:
                default:
                    return null;
            }
        }
    }
}
