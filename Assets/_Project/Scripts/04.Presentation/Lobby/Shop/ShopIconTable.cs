using UnityEngine;


namespace ParkingCat.Lobby.Shop
{
    [CreateAssetMenu(menuName="Shop/IconTable")]
    public sealed class ShopIconTable : ScriptableObject
    {
        [Header("Coin (by amount tier)")]
        public Sprite coin_6000;
        public Sprite coin_12000;
        public Sprite coin_250000;
        public Sprite coin_500000;
        public Sprite coin_1000000;
        public Sprite coin_2000000;

        [Header("Lives")]
        public Sprite heart;

        [Header("Boosters")]
        public Sprite boosterHint;
        public Sprite boosterUfo;

        [Header("Remove Ads")]
        public Sprite removeAds;

        [Header("Double Reward")]
        public Sprite doubleReward;

        public Sprite GetCoinSprite(int amount)
        {
            if (amount >= 200000) return coin_2000000;
            if (amount >= 100000)  return coin_1000000;
            if (amount >= 50000)  return coin_500000;
            if (amount >= 25000)  return coin_250000;
            if (amount >= 12000)   return coin_12000;
            return coin_6000;
        }
    }
}