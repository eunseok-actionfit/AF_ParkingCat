using System;
using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.UseCase.Economy.Lives;


namespace ParkingCat.Economy
{
    public sealed class LivesViewModel : IDisposable
    {
        public ObservableProperty<int> CurrentLives { get; } = new();
        public ObservableProperty<TimeSpan> UnlimitedRemaining { get; } = new();
        public ObservableProperty<int> MaxLives { get; } = new();
        public ObservableProperty<TimeSpan> NextRegenRemaining { get; } = new();
        public ObservableProperty<bool> IsInfinityMode { get; } = new();
        
        public bool IsFull { get; private set; }

        readonly GetLivesStatusUseCase _getLivesStatusUseCase;
        bool _isRunning;

        public LivesViewModel(GetLivesStatusUseCase getLivesStatusUseCase)
        {
            _getLivesStatusUseCase = getLivesStatusUseCase;
            StartLoop().Forget();
        }

        async UniTaskVoid StartLoop()
        {
            _isRunning = true;

            await UpdateOnce();

            while (_isRunning)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1));
                await UpdateOnce();
            }
        }

        async UniTask UpdateOnce()
        {
            var status = await _getLivesStatusUseCase.ExecuteAsync();

            CurrentLives.Value = status.Lives;
            MaxLives.Value = status.MaxLives;
            NextRegenRemaining.Value = status.NextRegenRemaining;
            UnlimitedRemaining.Value = status.UnlimitedRemaining;
            IsInfinityMode.Value = status.IsUnlimited;
            
            IsFull = status.Lives >= status.MaxLives;
        }

        public UniTask RefreshOnceAsync() => UpdateOnce();

        public void Dispose()
        {
            _isRunning = false;
        }
    }
}