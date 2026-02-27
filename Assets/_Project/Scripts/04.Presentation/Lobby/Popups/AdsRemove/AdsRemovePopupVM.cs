using System;
using AES.Tools;
using AES.Tools.UI.Popups;
using ParkingCat.Lobby.Shop;


namespace ParkingCat.Lobby.Popups.AdsRemove
{
    public sealed class AdsRemovePopupVM : PopupViewModelBase
    {
        public Command CloseCommand { get; }
        public AsyncCommand BuyCommand { get; }
        public ShopItemViewModel Item { get; }

        readonly IDisposable _priceSub;
        readonly string _productKey = "removeads7day_99";

        public AdsRemovePopupVM(IapDatabase db, ShopIconTable icons, ShopThemeTable themes)
        {
            CloseCommand = new Command(Close);

            Item = ShopItemVmBuilder.BuildShopItemVM(_productKey, db, icons, themes);
            _priceSub = ShopItemVmBuilder.BindPriceUpdates(Item);

            // 성공(confirmed) 이벤트에서만 닫기
            AES.Tools.IAP.PurchaseConfirmedByProductKey += OnPurchaseConfirmed;

            // 구매 시작만 (여기서 Close 하지 않음)
            BuyCommand = new AsyncCommand(
                executeAsync: _ => AES.Tools.IAP.PurchaseByProductKeyAsync(_productKey),
                canExecute: Item.BuyCommand.CanExecute
            );
        }

        void OnPurchaseConfirmed(string productKey)
        {
            if (!string.Equals(productKey, _productKey, StringComparison.Ordinal))
                return;

            Close(); // confirmed일 때만 닫힘
        }

        public override void Dispose()
        {
            AES.Tools.IAP.PurchaseConfirmedByProductKey -= OnPurchaseConfirmed;
            _priceSub?.Dispose();
            base.Dispose();
        }
    }
}