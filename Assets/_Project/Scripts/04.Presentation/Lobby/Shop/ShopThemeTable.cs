using AYellowpaper.SerializedCollections;
using UnityEngine;


namespace ParkingCat.Lobby.Shop
{
    [CreateAssetMenu(menuName="Shop/ThemeTable")]
    public sealed class ShopThemeTable : ScriptableObject
    {
        public Sprite DefaultBundleBg;
        public SerializedDictionary<string, Sprite> BundleBgByKey;
    
        public Sprite GetBundleBg(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && BundleBgByKey.TryGetValue(key, out var s) && s != null)
                return s;
            return DefaultBundleBg;
        }
    }
}