using Cysharp.Threading.Tasks;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.Economy
{
    public class GetAdsRemoveUseCase
    {
        readonly IPlayerEconomyRepository _repo;
        
        public GetAdsRemoveUseCase(IPlayerEconomyRepository repo)
        {
            _repo = repo;
        }
        
        public async UniTask<bool> ExecuteAsync()
        {
            var eco = await _repo.LoadAsync();
            return eco.RemoveAds;
        }
    }
}


