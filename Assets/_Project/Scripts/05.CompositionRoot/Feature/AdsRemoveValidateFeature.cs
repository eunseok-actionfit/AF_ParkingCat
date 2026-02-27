#nullable enable

using System;
using AES.Tools;
using AES.Tools.VContainer.Bootstrap.Framework;
using Cysharp.Threading.Tasks;
using ParkingCat.UseCase.Economy;
using UnityEngine;
using UnityEngine.Purchasing.Security;
using VContainer;
using VContainer.Unity;

namespace ParkingCat.Feature
{
    [CreateAssetMenu(menuName = "Bootstrap/Features/AdsRemoveValidateFeature", fileName = "AdsRemoveValidateFeature")]
    public sealed class AdsRemoveValidateFeature : AppFeatureSO
    {
        // IapDatabase에 정의된 "광고제거" productId
        private const string RemoveAdsProductId = "removeads7day_99";

        // 중복 구독 방지
        private static bool _subscribed;
        private static bool _applied;

        public override UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
        {
            Debug.Log("[IAP] AdsRemoveValidateFeature Initialize");

            if (_subscribed)
                return UniTask.CompletedTask;

            _subscribed = true;

            // 중요: Orders 기반 복원 이벤트(= OwnedNonConsumableFound)에서만 적용
            // UnityIapBackend 인스턴스를 resolve 할 수 있으면 그 이벤트에 바로 붙는게 가장 안전함.
            // (정적 이벤트보다 인스턴스 이벤트가 수명 관리가 쉬움)
            // var backend = rootScope.Container.Resolve<IIapPurchaseBackend>() as UnityIapBackend;
            // if (backend == null)
            // {
            //     Debug.LogError("[IAP] UnityIapBackend not found in container (IIapPurchaseBackend resolve failed).");
            //     return UniTask.CompletedTask;
            // }
            
            UnityIapBackend.OwnedNonConsumableFound += (productId, receipt) =>
            {
                if (_applied) return;
                if (!string.Equals(productId, RemoveAdsProductId, StringComparison.Ordinal))
                    return;

                // // receipt 검증 통과 시에만 적용
                // if (!ValidateReceipt(receipt, RemoveAdsProductId))
                // {
                //     Debug.Log("[IAP] RemoveAds receipt invalid (ignored).");
                //     return;
                // }

                _applied = true;
                ApplyRemoveAds(rootScope).Forget();
            };

            // 앱 시작 시 이미 purchases fetched가 끝난 케이스를 대비해서
            // "현재 상태에서 적용 가능한지"를 한번 더 체크하고 싶다면,
            // backend.WaitForPurchasesFetched() 이후 Orders 캐시를 두고 재평가하는 방식이 필요함.
            // (UnityIapBackend에서 Orders 캐시를 지원하면 여기서 즉시 Apply 가능)

            return UniTask.CompletedTask;
        }

//         private static bool ValidateReceipt(string receipt, string productId)
//         {
//             try
//             {
//                 var validator = new CrossPlatformValidator(
//                     GooglePlayTangle.Data(),
//                     AppleTangle.Data(),
//                     Application.identifier);
//
//                 var result = validator.Validate(receipt);
//
// #if UNITY_ANDROID
//                 GooglePlayReceipt latest = null;
//                 foreach (var r in result)
//                 {
//                     if (r is not GooglePlayReceipt g || g.productID != productId)
//                         continue;
//
//                     if (latest == null || g.purchaseDate > latest.purchaseDate)
//                         latest = g;
//                 }
//                 return latest is { purchaseState: GooglePurchaseState.Purchased };
//
// #elif UNITY_IOS
//                 AppleInAppPurchaseReceipt latest = null;
//                 foreach (var r in result)
//                 {
//                     if (r is not AppleInAppPurchaseReceipt a || a.productID != productId)
//                         continue;
//
//                     if (latest == null || a.purchaseDate > latest.purchaseDate)
//                         latest = a;
//                 }
//                 return latest != null && latest.cancellationDate == DateTime.MinValue;
// #else
//                 return true;
// #endif
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[IAP] ValidateReceipt exception: {e}");
//                 return false;
//             }
//         }

        private static async UniTaskVoid ApplyRemoveAds(LifetimeScope rootScope)
        {
            Debug.Log("[IAP] Valid remove ads receipt. Applying remove ads...");

            var removeAdsUseCase = rootScope.Container.Resolve<RemoveAdsUseCase>();
            await removeAdsUseCase.ExecuteAsync();
        }
    }
}
