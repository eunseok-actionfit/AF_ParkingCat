using System.Collections.Generic;
using AES.Tools;
using ParkingCat.Game.Car;
using UnityEngine;

namespace ParkingCat.Game.TrafficLight
{
    public sealed class TrafficLightPenaltyTrigger : MonoBehaviour
    {
        [SerializeField] int extraCost = 2;
        
       // void OnDisable() => _enteredCars.Clear();

        void OnTriggerEnter2D(Collider2D other)
        {
            var carView = other.GetComponentInParent<CarView>();
            if (carView == null) return;

            int carId = carView.CarId;
            if (carId <= 0) return;
            
            EventBus<TrafficLightPenaltyEvent>.Raise(new TrafficLightPenaltyEvent(carId, extraCost));
        }
    }
}