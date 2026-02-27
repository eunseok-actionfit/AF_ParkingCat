using System;


namespace ParkingCat.Entities.Economy
{
    public sealed class LivesConfig
    {
        public int MaxLives { get; }
        public TimeSpan RegenInterval { get; }
        public int InitialLives { get; }

        public LivesConfig(int maxLives, TimeSpan regenInterval, int initialLives)
        {
            MaxLives = maxLives;
            RegenInterval = regenInterval;
            InitialLives = initialLives;
        }
    }
}