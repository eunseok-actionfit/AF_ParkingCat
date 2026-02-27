using System;
using System.Collections.Generic;
using AES.Tools;
using ParkingCat.Entities;
using ParkingCat.Game.Car;
using UnityEngine;


[CreateAssetMenu(menuName = "AES/Converters/VM To Car Prefab")]
public sealed class CarPrefabConverter : ValueConverterSOBase
{
    [Serializable]
    public struct Entry
    {
        public CarType Key;
        public MonoContext Prefab;
    }

    [SerializeField] private List<Entry> entries;
    [SerializeField] private MonoContext defaultPrefab;

    public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value == null || value is not CarItemViewModel vm)
            return defaultPrefab;
        
        foreach (var e in entries)
        {
            if (e.Key == vm.Type.Value)
                return e.Prefab;
        }

        return defaultPrefab;
    }
}