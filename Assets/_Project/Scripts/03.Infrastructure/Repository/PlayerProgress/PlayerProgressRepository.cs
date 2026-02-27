using AES.Tools;
using Cysharp.Threading.Tasks;


namespace ParkingCat.Repository.PlayerProgress
{
    public sealed class PlayerProgressRepository : IPlayerProgressRepository
    {
        readonly IStorageService _storage;
        readonly ISlotService _slot;

        public Entities.PlayerProgress Current { get; private set; } 

        public PlayerProgressRepository(IStorageService storage, ISlotService slot)
        {
            _storage = storage;
            _slot = slot;
        }

        public async UniTask<Entities.PlayerProgress> LoadAsync()
        {
            var slotId = _slot.CurrentSlotId;
            var dto = await _storage.LoadAsync<PlayerProgressData>(slotId) ?? new PlayerProgressData();

            Current = new Entities.PlayerProgress(dto.lastClearedLevel); 
            return Current;
        }

        public async UniTask SaveAsync(Entities.PlayerProgress progress)
        {
            var dto = new PlayerProgressData
            {
                lastClearedLevel = progress.LastClearedLevel
            };

            var slotId = _slot.CurrentSlotId;
            await _storage.SaveAsync(slotId, dto);

            Current = progress;
        }
    }
}