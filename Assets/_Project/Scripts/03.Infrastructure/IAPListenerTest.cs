using System;
using AES.Tools;
using AES.Tools.VContainer.Bootstrap;
using Cysharp.Threading.Tasks;
using ParkingCat.UseCase.Economy;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using VContainer;
using VContainer.Unity;


namespace ParkingCat
{
    public class IAPListenerTest : MonoBehaviour
    {

        [SerializeField] private IAPListener iapListener;
        
        
        // [Inject] RemoveAdsUseCase removeAdsUseCase;
        // const string RemoveAdsProductId = "removeads7day_99";
        void Start()
        {
            iapListener.onPurchasesFetched.AddListener(call =>
            {
                Debug.Log($"[IAP] Purchases fetched: confirmed={call.ConfirmedOrders.Count}, deferred={call.DeferredOrders.Count}, pending={call.PendingOrders.Count}");
            
                foreach (var confirmed in call.ConfirmedOrders)
                {
                    Debug.Log($"[IAP] Purchase fetched: {confirmed.Info.Receipt}");
                    
            
                    // if (IAP.ValidateReceiptByProductKey(RemoveAdsProductId, confirmed.Info.Receipt, GooglePlayTangle.Data(), AppleTangle.Data()))
                    // {
                    //     removeAdsUseCase.ExecuteAsync().Forget();
                    // }
                    // else
                    // {
                    //     Debug.Log("[IAP] RemoveAds receipt invalid.");
                    // }
                }
            });

        }

    }
}