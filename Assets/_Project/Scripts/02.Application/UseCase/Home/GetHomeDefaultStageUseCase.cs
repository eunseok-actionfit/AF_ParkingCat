using Cysharp.Threading.Tasks;
using ParkingCat.Repository;
using ParkingCat.ValueObjects.StageCatalog;


namespace ParkingCat.UseCase.Home
{
    public sealed class GetHomeDefaultStageUseCase 
    {
        readonly IPlayerProgressRepository _progressRepository;
        readonly IStageCatalog _catalog;

        public GetHomeDefaultStageUseCase(
            IPlayerProgressRepository progressRepository,
            IStageCatalog catalog)
        {
            _progressRepository = progressRepository;
            _catalog = catalog;
        }

        public async UniTask<int> ExecuteAsync()
        {
            // 저장된 진행도 불러오기
            var progress = await _progressRepository.LoadAsync();

            var minStage = _catalog.MinLevel;
            var maxStage = _catalog.MaxLevel;

            // PlayerProgress 내부 규칙을 이용해 기본 스테이지 결정
            return progress.GetDefaultStage(minStage, maxStage);
        }
    }
}