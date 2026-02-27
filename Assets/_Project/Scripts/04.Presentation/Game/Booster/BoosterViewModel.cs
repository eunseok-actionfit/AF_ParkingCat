using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.UseCase.Booster;


namespace ParkingCat.Game.Booster
{
    public sealed class BoosterViewModel
    {
        public BoosterId Id { get; }

        public ObservableProperty<int> Count { get; } = new();

        readonly GetBoosterCountUseCase _getCountUseCase;
        readonly AddBoosterUseCase _addBoosterUseCase;
        readonly ConsumeBoosterUseCase _consumeBoosterUseCase;

        public BoosterViewModel(
            BoosterId id,
            GetBoosterCountUseCase getCountUseCase,
            AddBoosterUseCase addBoosterUseCase,
            ConsumeBoosterUseCase consumeBoosterUseCase)
        {
            Id = id;
            _getCountUseCase = getCountUseCase;
            _addBoosterUseCase = addBoosterUseCase;
            _consumeBoosterUseCase = consumeBoosterUseCase;

            RefreshAsync().Forget();
        }

        public async UniTask RefreshAsync()
        {
            Count.Value = await _getCountUseCase.ExecuteAsync(Id);
        }

        public async UniTask<bool> TryConsumeAsync()
        {
            var ok = await _consumeBoosterUseCase.ExecuteAsync(Id);
            RefreshAsync().Forget();
            return ok;
        }
        
        public async UniTask AddAsync(int amount)
        {
            await _addBoosterUseCase.ExecuteAsync(Id, amount);
            RefreshAsync().Forget();
        }
    }
}