using Cysharp.Threading.Tasks;


namespace ParkingCat.Repository.PlayerState
{
    public class PlayerStateRepository : IPlayerStateRepository
    {
        readonly IPlayerProgressRepository _playerProgressRepo;
        readonly IPlayerEconomyRepository _playerEconomyRepo;

        public PlayerStateRepository(
            IPlayerProgressRepository playerProgressRepo,
            IPlayerEconomyRepository playerEconomyRepo)
        {
            _playerProgressRepo = playerProgressRepo;
            _playerEconomyRepo = playerEconomyRepo;
        }


        public async UniTask<Entities.PlayerState> LoadAsync()
        {
            var player = await _playerProgressRepo.LoadAsync();
            var economy = await _playerEconomyRepo.LoadAsync();

            return new Entities.PlayerState(player, economy);
        }

        public async UniTask SaveAsync(Entities.PlayerState state)
        {
            await _playerProgressRepo.SaveAsync(state.Progress);
            await _playerEconomyRepo.SaveAsync(state.Economy);
        }
    }
}