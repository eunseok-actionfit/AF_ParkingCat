using AES.Tools;
using AYellowpaper.SerializedCollections;
using ParkingCat.Entities.Booster;
using UnityEngine;


namespace ParkingCat
{
    [CreateAssetMenu(
        fileName = "BoosterSettings",
        menuName = "Game/Booster Settings")]
    public sealed class BoosterSettings : ScriptableObject
    {
        [Header("초기 보유 개수")]
        [SerializeField]
        SerializedDictionary<BoosterId, int> initialCounts;

        // 인스펙터에서 우클릭 메뉴로 채우는 용도
        [ContextMenu("Fill InitialCounts From Enum Attributes")]
        void FillInitialCounts()
        {
            var dict = DictFromAttributes.CreateFromEnum<BoosterId, int>();
            initialCounts = dict;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        void OnValidate()
        {
            if (initialCounts == null || initialCounts.Count == 0)
                initialCounts = DictFromAttributes.CreateFromEnum<BoosterId, int>();
        }

        public int GetInitialCount(BoosterId id)
        {
            return initialCounts != null && initialCounts.TryGetValue(id, out var c)
                ? c
                : 0;
        }
    }
}