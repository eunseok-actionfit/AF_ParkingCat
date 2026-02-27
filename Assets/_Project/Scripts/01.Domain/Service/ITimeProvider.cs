using System;


namespace ParkingCat.Service
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}

