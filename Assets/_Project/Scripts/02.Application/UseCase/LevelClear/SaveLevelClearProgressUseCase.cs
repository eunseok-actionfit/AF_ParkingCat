using Cysharp.Threading.Tasks;
using ParkingCat.Repository;


namespace ParkingCat.UseCase.LevelClear
{
    public sealed class SaveLevelClearProgressUseCase
    {
        readonly IPlayerProgressRepository _progressRepository;

        public SaveLevelClearProgressUseCase(IPlayerProgressRepository progressRepository)
        {
            _progressRepository = progressRepository;
        }

        public async UniTask ExecuteAsync(int levelId)
        {
            var progress = await _progressRepository.LoadAsync();

            if (levelId > progress.LastClearedLevel)
            {
                progress.UpdateLastCleared(levelId);
                await _progressRepository.SaveAsync(progress);
            }
        }
    }
}