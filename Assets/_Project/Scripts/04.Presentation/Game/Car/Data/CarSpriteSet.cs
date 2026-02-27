using UnityEngine;


namespace ParkingCat.Game.Car.Data
{
    [CreateAssetMenu(menuName = "ParkingCat/Visuals/CarSpriteSet", fileName = "CarSpriteSet")]
    public sealed class CarSpriteSet : ScriptableObject
    {
        public Sprite up;
        public Sprite right;
        public Sprite down;
        public Sprite left;

        public Sprite Get(Dir4 dir) => dir switch
        {
            Dir4.Up    => up,
            Dir4.Right => right,
            Dir4.Down  => down,
            Dir4.Left  => left,
            _ => up
        };
    }
}