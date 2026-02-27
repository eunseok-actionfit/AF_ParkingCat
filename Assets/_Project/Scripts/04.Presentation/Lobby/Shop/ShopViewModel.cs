using System;
using System.Linq;
using AES.Tools;
using UnityEngine;


namespace ParkingCat.Lobby.Shop
{
    public sealed class ShopViewModel
    {
        public ObservableList<ShopItemViewModel> SpecialItems { get; } = new();
        public ObservableList<ShopItemViewModel> BundleItems  { get; } = new();
        public ObservableList<ShopItemViewModel> CoinItems    { get; } = new();

        readonly IapDatabase _db;
        readonly ShopIconTable _icons;
        readonly ShopThemeTable _themes;

        public ShopViewModel(IapDatabase db, ShopIconTable icons, ShopThemeTable themes)
        {
            Debug.Log($"[ShopViewModel] ctor hash={GetHashCode()}");
            _db = db;
            _icons = icons;
            _themes = themes;

            // 가격 갱신은 기존처럼 리스트 전체에 적용(Builder는 생성 시 캐시가격만 반영)
            AES.Tools.IAP.PriceUpdatedByProductKey += SetPriceByProductKey;
            AES.Tools.IAP.Ready += RefreshAllCachedPrices;

            BuildLists();
            RefreshAllCachedPrices();
        }

        void BuildLists()
        {
            SpecialItems.Clear();
            BundleItems.Clear();
            CoinItems.Clear();

            var items = _db.EnumerateActiveProductIdsForCurrentPlatform()
                .Select(x =>
                {
                    if (!_db.TryGetProduct(x.productKey, out var meta)) return default;
                    if (!meta.IsVisible) return default;
                    return (ok: true, x.productKey, meta);
                })
                .Where(x => x.ok)
                .OrderBy(x => x.meta.SortOrder)
                .ToList();

            foreach (var it in items)
            {
                // 공용 Builder로 단품 VM 구성(타이틀/보상/비주얼/캐시가격)
                var vm = ShopItemVmBuilder.BuildShopItemVM(it.productKey, _db, _icons, _themes);

                switch (it.meta.Category)
                {
                    case "Special": SpecialItems.Add(vm); break;
                    case "Bundle":  BundleItems.Add(vm);  break;
                    case "Coin":    CoinItems.Add(vm);    break;
                }
            }
        }

        void RefreshAllCachedPrices()
        {
            RefreshList(SpecialItems);
            RefreshList(BundleItems);
            RefreshList(CoinItems);
        }

        void RefreshList(ObservableList<ShopItemViewModel> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var vm = list[i];
                if (vm == null) continue;

                if (AES.Tools.IAP.TryGetLocalizedPriceByProductKey(vm.ProductKey, out var price) && !string.IsNullOrWhiteSpace(price))
                {
                    vm.PriceText.Value = price;
//                    Debug.Log($"[IAP ShopViewModel] Refresh cached: {vm.ProductKey} => {price}");
                }
            }
        }

        public void SetPriceByProductKey(string productKey, string priceText)
        {
          //  Debug.Log($"[ShopViewModel] SetPriceByProductKey hash={GetHashCode()} {productKey} => {priceText}");
            if (string.IsNullOrWhiteSpace(productKey)) return;

            SetPriceByProductKey(SpecialItems, productKey, priceText);
            SetPriceByProductKey(BundleItems, productKey, priceText);
            SetPriceByProductKey(CoinItems, productKey, priceText);
        }

        static void SetPriceByProductKey(ObservableList<ShopItemViewModel> list, string productKey, string priceText)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var vm = list[i];
                if (vm != null && string.Equals(vm.ProductKey, productKey, StringComparison.Ordinal))
                {
                    vm.PriceText.Value = priceText;
                  //  Debug.Log($"[IAP ShopViewModel] Price applied: {productKey} => {priceText}");
                    return;
                }
            }
            
            //Debug.Log($"[IAP ShopViewModel] Price arrived before VM build: {productKey} => {priceText}");
        }
    }
}
