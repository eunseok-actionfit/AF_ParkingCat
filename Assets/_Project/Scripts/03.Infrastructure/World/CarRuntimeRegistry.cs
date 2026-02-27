using System.Collections.Generic;
using ParkingCat.Entities;
using UnityEngine;


namespace ParkingCat.World
{
    public sealed class CarRuntimeRegistry : ICarRuntimeRegistry
    {
        readonly Dictionary<int, CarRuntimeInfo> _map = new();

        public int Count => _map.Count;

        public IEnumerable<CarRuntimeInfo> All => _map.Values; // ★ 추가

        public bool TryGet(int carId, out CarRuntimeInfo info)
            => _map.TryGetValue(carId, out info);

        public void Register(int carId, Transform posTf,Transform rotTf, ValueObjects.TurnProgram program, int turnCursor, CarDims dims)
        {
            _map[carId] = new CarRuntimeInfo(
                carId: carId,
                posTf: posTf,
                rotTf: rotTf,
                program: program,
                cursor: turnCursor,
                dims: dims,
                isLeaving: false);
        }

        public void Unregister(int carId)
            => _map.Remove(carId);

        public void SetTurnState(int carId, ValueObjects.TurnProgram program, int turnCursor)
        {
            if (!_map.TryGetValue(carId, out var info)) return;
            _map[carId] = new CarRuntimeInfo(
                carId: carId,
                posTf: info.PosTf,
                rotTf: info.RotTf,
                program: program,
                cursor: turnCursor,
                dims: info.Dims,
                isLeaving: info.IsLeaving);
        }

        public void SetLeaving(int carId, bool isLeaving)
        {
            if (!_map.TryGetValue(carId, out var info)) return;
            _map[carId] = new CarRuntimeInfo(
                carId: carId,
                posTf: info.PosTf,
                rotTf: info.RotTf,
                program: info.Program,
                cursor: info.TurnCursor,
                dims: info.Dims,
                isLeaving: isLeaving);
        }
    }
}