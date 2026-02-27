using System;
using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;


namespace ParkingCat.Repository.Booster
{
    public sealed class BoosterInventoryRepository : IBoosterInventoryRepository
    {
        readonly IStorageService _storage;
        readonly ISlotService _slot;
        readonly BoosterSettings _settings;

        public BoosterInventoryRepository(
            IStorageService storage,
            ISlotService slot,
            BoosterSettings settings)
        {
            _storage  = storage;
            _slot     = slot;
            _settings = settings;
        }

        public async UniTask<BoosterInventory> LoadAsync()
        {
            var slotId = _slot.CurrentSlotId;
            var data = await _storage.LoadAsync<BoosterInventoryData>(slotId);

            // 1) 저장 데이터가 전혀 없으면 → BoosterSettings 기준 전체 초기화
            if (data == null)
            {
                var inv = new BoosterInventory();

                foreach (BoosterId id in Enum.GetValues(typeof(BoosterId)))
                {
                    var count = _settings.GetInitialCount(id);
                    if (count > 0)
                        inv.SetCount(id, count);
                }

                await SaveAsync(inv);
                return inv;
            }

            // 2) 저장 데이터가 있다면 → 먼저 DTO -> 도메인 변환
            var inventory = new BoosterInventory();

            // 존재하는 부스터들 채우기
            foreach (var e in data.boosters)
            {
                if (Enum.TryParse<BoosterId>(e.id, out var id))
                {
                    inventory.SetCount(id, e.count);
                }
                else
                {
                    // 알 수 없는 id는 무시(또는 로그)
                }
            }

            bool changed = false;

            // 3) enum에 있지만 저장 데이터에 없는 "신규 BoosterId" 들 찾아서 기본값 채우기
            foreach (BoosterId id in Enum.GetValues(typeof(BoosterId)))
            {
                // 이미 인벤토리에 있으면 패스
                if (inventory.GetCount(id) > 0 || data.boosters.Exists(x => x.id == id.ToString()))
                    continue;

                var initialCount = _settings.GetInitialCount(id);
                if (initialCount > 0)
                {
                    inventory.SetCount(id, initialCount);
                    changed = true;
                }
            }

            // 4) 새 아이템이 추가된 경우만 저장
            if (changed)
                await SaveAsync(inventory);

            return inventory;
        }

    
        public async UniTask SaveAsync(BoosterInventory inv)
        {
            var data = new BoosterInventoryData();

            foreach (var kv in inv.AsReadOnly())
            {
                data.boosters.Add(new BoosterInventoryEntryData
                {
                    id = kv.Key.ToString(),
                    count = kv.Value
                });
            }

            var slotId = _slot.CurrentSlotId;
            await _storage.SaveAsync(slotId, data);
        }
    }
}