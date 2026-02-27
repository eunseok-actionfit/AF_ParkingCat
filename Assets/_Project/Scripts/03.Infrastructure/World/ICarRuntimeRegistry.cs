using System.Collections.Generic;
using ParkingCat.Entities;
using UnityEngine;


namespace ParkingCat.World
{
    public interface ICarRuntimeRegistry
    {
        bool TryGet(int carId, out CarRuntimeInfo info);
        IEnumerable<CarRuntimeInfo> All { get; }  
        int Count { get; }

        void Register(int carId, Transform posTf,Transform rotTf, ValueObjects.TurnProgram program, int turnCursor, CarDims dims);
        void Unregister(int carId);

        void SetTurnState(int carId, ValueObjects.TurnProgram program, int turnCursor);

        // Leaving 정책 통합
        void SetLeaving(int carId, bool isLeaving); // ★ 추가
    }

    public readonly struct CarRuntimeInfo
    {
        public readonly int CarId;
        public readonly Transform PosTf;
        public readonly Transform RotTf;
        public readonly ValueObjects.TurnProgram Program;
        public readonly int TurnCursor;
        public readonly CarDims Dims;     // (length, width)
        public readonly bool IsLeaving;  

        public CarRuntimeInfo(
            int carId,
         Transform posTf,
         Transform rotTf,
            ValueObjects.TurnProgram program,
            int cursor,
            CarDims dims,
            bool isLeaving)
        {
            CarId = carId;
            PosTf = posTf;
            RotTf = rotTf;
            Program = program;
            TurnCursor = cursor;
            Dims = dims;
            IsLeaving = isLeaving;
        }
    }
}