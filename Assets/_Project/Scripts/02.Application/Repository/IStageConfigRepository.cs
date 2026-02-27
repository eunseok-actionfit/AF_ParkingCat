using Cysharp.Threading.Tasks;
using ParkingCat.Config;


namespace ParkingCat.Repository
{
    public interface IStageConfigRepository
    {
        UniTask<StageConfig> LoadAsync(int stageId);
    }
}