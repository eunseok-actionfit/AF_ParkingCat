
using ParkingCat.Entities;
using UnityEngine;


namespace ParkingCat.Sim
{
    public struct CarSimState
    {
        public int CarId;

        public Vector2 Pos;
        public float HeadingRad;

        public ValueObjects.TurnProgram TurnProgram;
        public int TurnCursor;

        public CarDims Dims;
        public float Speed;

        public static CarSimState FromDomain(Car car)
        {
            return new CarSimState
            {
                CarId = car.Id,
                Pos = car.Pos,
                HeadingRad = car.HeadingRad,
                TurnProgram = car.TurnProgram, // 정책: 공유 (불변 전제)
                TurnCursor = car.TurnCursor,
                Dims = car.Dims,
                Speed = car.Speed
            };
        }
    }
}