using System;
using System.Collections.Generic;
using ParkingCat.Entities.Booster;


namespace ParkingCat.Game.Booster
{
    public sealed class BoosterBarViewModel
    {
        public BoosterViewModel EscapeHint      { get; }
        public BoosterViewModel ForceExit   { get; }

        readonly Dictionary<BoosterId, BoosterViewModel> _byId;

        public BoosterBarViewModel( Func<BoosterId, BoosterViewModel> factory)
        {
            EscapeHint      = factory.Invoke(BoosterId.EscapeHint);
            ForceExit   = factory.Invoke(BoosterId.ForceExit);

            _byId = new Dictionary<BoosterId, BoosterViewModel>
            {
                { BoosterId.EscapeHint, EscapeHint },
                { BoosterId.ForceExit, ForceExit },
            };
        }

        public BoosterViewModel Get(BoosterId id) => _byId[id];
    }
}