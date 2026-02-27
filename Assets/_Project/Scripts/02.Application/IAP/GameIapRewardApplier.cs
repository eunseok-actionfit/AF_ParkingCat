using System;
using System.Collections.Generic;
using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.Entities.Booster;
using ParkingCat.UseCase.Booster;
using ParkingCat.UseCase.Economy;
using ParkingCat.UseCase.Economy.Coin;
using ParkingCat.UseCase.Economy.Lives;


namespace ParkingCat
{
    public sealed class GameIapRewardApplier : IIapRewardApplier
    {
        readonly AddCoinUseCase _addCoin;
        readonly AddBoosterUseCase _addBooster;
        readonly AddUnlimitedLivesMinutesUseCase _addUnlimitedLives;
        readonly RemoveAdsUseCase _removeAds;

        public GameIapRewardApplier(
            AddCoinUseCase addCoin,
            AddBoosterUseCase addBooster,
            AddUnlimitedLivesMinutesUseCase addUnlimitedLives,
            RemoveAdsUseCase removeAds)
        {
            _addCoin = addCoin;
            _addBooster = addBooster;
            _addUnlimitedLives = addUnlimitedLives;
            _removeAds = removeAds;
        }

        public async UniTask ApplyAsync(IReadOnlyList<IapBundleContentRow> rewards)
        {
            if (rewards == null || rewards.Count == 0)
                return;

            for (int i = 0; i < rewards.Count; i++)
            {
                var r = rewards[i];
                if (r == null) continue;

                var type = (r.ItemType ?? "").Trim();
                var amount = Math.Max(0, r.Amount);

                switch (type)
                {
                    case "Coin":
                        if (amount > 0) await _addCoin.ExecuteAsync(amount);
                        break;

                    case "UnlimitedLivesMin":
                        if (amount > 0) await _addUnlimitedLives.ExecuteAsync(amount);
                        break;

                    case "Boosters":
                        if (amount > 0)
                        {
                            await _addBooster.ExecuteAsync(BoosterId.EscapeHint, amount);
                            await _addBooster.ExecuteAsync(BoosterId.ForceExit, amount);
                        }
                        break;

                    case "RemoveAds":
                        await _removeAds.ExecuteAsync();
                        break;
                }
            }
        }
        
    }
}
