using ParkingCat.Stage.DTOs;

namespace ParkingCat.World
{
    public interface IOneWaySetter
    {
        void SetOneWay(StageRunDto.OneWayLaneDto[] oneWays);
    }
}