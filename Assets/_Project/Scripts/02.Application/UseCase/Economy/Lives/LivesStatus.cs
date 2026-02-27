using System;


namespace ParkingCat.UseCase.Economy.Lives
{
    public readonly struct LivesStatus
    {
        public readonly int Lives;
        public readonly int MaxLives;
        public readonly TimeSpan NextRegenRemaining;
        public readonly bool IsUnlimited;
        public readonly TimeSpan UnlimitedRemaining;

        public LivesStatus(
            int lives,
            int maxLives,
            TimeSpan nextRegenRemaining,
            bool isUnlimited,
            TimeSpan unlimitedRemaining)
        {
            Lives = lives;
            NextRegenRemaining = nextRegenRemaining;
            IsUnlimited = isUnlimited;
            UnlimitedRemaining = unlimitedRemaining;
            MaxLives = maxLives;
        }
    }
}