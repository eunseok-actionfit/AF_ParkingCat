using AES.Tools;

namespace ParkingCat.Game.TrafficLight
{
    public readonly struct TrafficLightPenaltyEvent : IEvent
    {
        public readonly int CarId;
        public readonly int ExtraCost;

        public TrafficLightPenaltyEvent(int carId, int extraCost)
        {
            CarId = carId;
            ExtraCost = extraCost;
        }
    }
}