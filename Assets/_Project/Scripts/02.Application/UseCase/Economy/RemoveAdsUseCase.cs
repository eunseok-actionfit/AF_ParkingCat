// RemoveAdsUseCase.cs
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using ParkingCat.Repository;
using UnityEngine;


namespace ParkingCat.UseCase.Economy
{
    public sealed class RemoveAdsUseCase
    {
        readonly IPlayerEconomyRepository _repo;

        public RemoveAdsUseCase(IPlayerEconomyRepository repo)
        {
            _repo = repo;
        }

        public async UniTask ExecuteAsync()
        {
            Debug.Log("[AdsRemoveUseCase] ExecuteAsync");
            var eco = await _repo.LoadAsync();
            eco.SetRemoveAds(true);
            
            await _repo.SaveAsync(eco);
            
            ADS.SetRuntimeDisabled(true);
        }
    }
}