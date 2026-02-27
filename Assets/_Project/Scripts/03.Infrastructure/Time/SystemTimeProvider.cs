using System;
using ParkingCat.Service;


namespace ParkingCat.Time
{
    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}