using UnityEngine;
using UnityEngine.Localization;

namespace ParkingCat.Game.StageFailed
{
    [CreateAssetMenu(menuName = "ParkingCat/Explanation/Entry", fileName = "ExplanationEntry")]
    public class ExplanationEntrySO : ScriptableObject
    {
        [field: SerializeField] public LocalizedString TitleLocKey { get; private set; }
        [field: SerializeField] public LocalizedString DescLocKey  { get; private set; }
        [field: SerializeField] public Sprite Image       { get; private set; }
        
        
    }
}