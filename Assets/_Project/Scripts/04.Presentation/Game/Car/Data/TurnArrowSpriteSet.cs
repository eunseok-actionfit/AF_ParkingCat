using UnityEngine;


namespace ParkingCat.Game.Car.Data
{
    [CreateAssetMenu(menuName = "ParkingCat/Visuals/TurnArrowSpriteSet", fileName = "TurnArrowSpriteSet")]
    public sealed class TurnArrowSpriteSet : ScriptableObject
    {
        public Sprite straight;
        public Sprite left;
        public Sprite right;
        public Sprite uturnLeft;
        public Sprite uturnRight;
        public Sprite leftRight;
        public Sprite rightLeft;
    }
}