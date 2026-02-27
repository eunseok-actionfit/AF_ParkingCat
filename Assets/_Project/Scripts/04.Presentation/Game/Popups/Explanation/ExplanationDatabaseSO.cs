using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace ParkingCat.Game.StageFailed
{
    [CreateAssetMenu(menuName = "ParkingCat/Explanation/Database", fileName = "ExplanationDatabase")]
    public class ExplanationDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private SerializedDictionary<ExplanationType, ExplanationEntrySO> entries = new();

        public ExplanationEntrySO GetEntry(ExplanationType type)
            => entries[type];

        public bool TryGetEntry(ExplanationType type, out ExplanationEntrySO entry)
            => entries.TryGetValue(type, out entry);
    }
}