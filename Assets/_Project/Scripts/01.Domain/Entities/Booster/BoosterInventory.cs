using System;
using System.Collections.Generic;


namespace ParkingCat.Entities.Booster
{


    public sealed class BoosterInventory
    {
        readonly Dictionary<BoosterId, int> _counts =
            new Dictionary<BoosterId, int>();

        public int GetCount(BoosterId id)
        {
            return _counts.GetValueOrDefault(id, 0);
        }

        public void SetCount(BoosterId id, int count)
        {
            _counts[id] = Math.Max(0, count);
        }

        public void Add(BoosterId id, int amount)
        {
            if (amount <= 0) return;
            var cur = GetCount(id);
            _counts[id] = cur + amount;
        }
        public void AddAll(int amount)
        {
            if (amount <= 0) return;
            
            var keys = new List<BoosterId>(_counts.Keys); 

            foreach (var id in keys)
            {
                _counts[id] += amount;
            }
        }


        public bool TryConsume(BoosterId id, int amount = 1)
        {
            if (amount <= 0) return true;
            var cur = GetCount(id);
            if (cur < amount) return false;

            _counts[id] = cur - amount;
            return true;
        }

        public IReadOnlyDictionary<BoosterId, int> AsReadOnly()
            => _counts;
    }
}