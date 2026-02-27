// App.Economy namespace 안에 파일 하나 추가
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Economy;
using ParkingCat.Repository;
using ParkingCat.Service;


namespace ParkingCat.UseCase.Home
{
    /// <summary>
    /// 외부 리워드(광고 등)로 라이프 1개를 지급.
    /// 이미 풀라이프면 false.
    /// </summary>
    public sealed class AddLifeFromRewardUseCase
    {
        readonly IPlayerEconomyRepository _repo;
        readonly ITimeProvider _time;
        readonly LivesConfig _config;

        public AddLifeFromRewardUseCase(
            IPlayerEconomyRepository repo,
            ITimeProvider time,
            LivesConfig config)
        {
            _repo   = repo;
            _time   = time;
            _config = config;
        }

        public async UniTask<bool> ExecuteAsync(int amount = 1)
        {
            var nowUtc  = _time.UtcNow;
            var economy = await _repo.LoadAsync();

            // 현재 시간 기준 자연 회복 먼저 반영
            economy.TickLives(nowUtc, _config.MaxLives, _config.RegenInterval);

            // 그 상태에서 라이프 +1 시도
            var ok = economy.TryAddLife(amount, nowUtc, _config.MaxLives);
            if (!ok)
                return false;

            await _repo.SaveAsync(economy);
            return true;
        }
    }
}