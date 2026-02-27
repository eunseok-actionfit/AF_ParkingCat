using System;
using System.Linq;
using AES.Tools;
// IapDatabase


// ShopItemViewModel

namespace ParkingCat.Lobby.Shop
{
    public static class ShopItemVmBuilder
    {
        public static ShopItemViewModel BuildShopItemVM(
            string productKey,
            IapDatabase db,
            ShopIconTable icons,
            ShopThemeTable themes)
        {
            var vm = new ShopItemViewModel(productKey);

            // title (프로젝트 규칙대로)
            vm.TitleKey.Value = $"shop.{productKey}.title";

            // rewards
            FillRewardPreviews(vm, db, productKey);

            // visuals
            vm.RebuildVisuals(icons, themes);

            // cached price 즉시 반영
            if (AES.Tools.IAP.TryGetLocalizedPriceByProductKey(productKey, out var price) &&
                !string.IsNullOrWhiteSpace(price))
            {
                vm.PriceText.Value = price;
            }

            return vm;
        }

        public static IDisposable BindPriceUpdates(ShopItemViewModel vm)
        {
            void Handler(string productKey, string priceText)
            {
                if (string.Equals(vm.ProductKey, productKey, StringComparison.Ordinal))
                    vm.PriceText.Value = priceText;
            }

            AES.Tools.IAP.PriceUpdatedByProductKey += Handler;
            return new ActionDisposable(() => AES.Tools.IAP.PriceUpdatedByProductKey -= Handler);
        }

        static void FillRewardPreviews(ShopItemViewModel vm, IapDatabase db, string productKey)
        {
            vm.RewardPreviews.Clear();

            var rewards = db.GetRewards(productKey);
            if (rewards == null || rewards.Count == 0)
                return;

            foreach (var r in rewards.OrderBy(x => x.DisplayOrder))
            {
                var kind = MapKind(r.ItemType);

                vm.RewardPreviews.Add(new ShopItemViewModel.RewardPreviewVM
                {
                    Kind = kind,
                    Amount = r.Amount,
                    ShowAmount = kind != ShopItemViewModel.RewardIconKind.RemoveAds,
                    IsMain = r.IsMain
                });
            }

            NormalizeLegacyBoosterRows(vm);
        }

        static ShopItemViewModel.RewardIconKind MapKind(string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
                return ShopItemViewModel.RewardIconKind.Unknown;

            switch (itemType.Trim())
            {
                case "Coin": return ShopItemViewModel.RewardIconKind.Coin;
                case "UnlimitedLivesMin": return ShopItemViewModel.RewardIconKind.UnlimitedLivesMin;
                case "DoubleStageRewardsMin": return ShopItemViewModel.RewardIconKind.DoubleRewardMin;
                case "Boosters": return ShopItemViewModel.RewardIconKind.Boosters;
                case "RemoveAds": return ShopItemViewModel.RewardIconKind.RemoveAds;
                default: return ShopItemViewModel.RewardIconKind.Unknown;
            }
        }

        static void NormalizeLegacyBoosterRows(ShopItemViewModel vm)
        {
            int maxBooster = 0;
            bool hadMainBooster = false;

            for (int i = vm.RewardPreviews.Count - 1; i >= 0; i--)
            {
                var p = vm.RewardPreviews[i];
                if (p.Kind != ShopItemViewModel.RewardIconKind.Boosters)
                    continue;

                maxBooster = Math.Max(maxBooster, p.Amount);
                hadMainBooster |= p.IsMain;
                vm.RewardPreviews.RemoveAt(i);
            }

            if (maxBooster > 0)
            {
                vm.RewardPreviews.Add(new ShopItemViewModel.RewardPreviewVM
                {
                    Kind = ShopItemViewModel.RewardIconKind.Boosters,
                    Amount = maxBooster,
                    ShowAmount = true,
                    IsMain = hadMainBooster
                });
            }
        }

        sealed class ActionDisposable : IDisposable
        {
            Action _dispose;
            public ActionDisposable(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
