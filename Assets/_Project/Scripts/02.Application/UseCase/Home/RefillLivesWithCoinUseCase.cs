// App.Economy namespace 안에 파일 하나 추가
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;


namespace ParkingCat.UseCase.Home
{
    /// <summary>
    /// 코인을 소비해서 라이프를 풀로 채운다.
    /// 코인이 부족하면 false.
    /// </summary>
    public sealed class RefillLivesWithCoinUseCase
    {
        readonly IPlayerEconomyRepository _repo;
        readonly ITimeProvider _time;
        readonly LivesConfig _config;

        public const int DefaultCost = 900;

        public RefillLivesWithCoinUseCase(
            IPlayerEconomyRepository repo,
            ITimeProvider time,
            LivesConfig config)
        {
            _repo   = repo;
            _time   = time;
            _config = config;
        }

        public async UniTask<bool> ExecuteAsync(int cost = DefaultCost)
        {
            var economy = await _repo.LoadAsync();

            // 코인 부족
            if (!economy.TrySpendCoin(cost))
                return false;

            // 라이프 풀 채우기
            economy.SetLivesFull(_time.UtcNow, _config.MaxLives);

            await _repo.SaveAsync(economy);
            return true;
        }
    }
}