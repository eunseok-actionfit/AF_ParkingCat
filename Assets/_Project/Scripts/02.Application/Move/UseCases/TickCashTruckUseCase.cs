using ParkingCat.Entities;
using ParkingCat.Repository;

public sealed class TickCashTruckUseCase
{
    readonly IStageRunRepository _repo;
    const float Interval = 5f;
    const int Loss = 10;

    public TickCashTruckUseCase(IStageRunRepository repo) => _repo = repo;

    public void Execute(float dt)
    {
        if (dt <= 0) return;

        var run = _repo.Get();
        var cars = run.Stage.Cars;

        for (int i = 0; i < cars.Count; i++)
        {
            var c = cars[i];
            if (c == null || c.IsLeaving) continue;
            if (!c.IsCashTruck) continue;
            if (c.CashGold <= 0) continue;

            c.CashDecayElapsed += dt;

            while (c.CashDecayElapsed >= Interval && c.CashGold > 0)
            {
                c.CashDecayElapsed -= Interval;
                c.CashGold = UnityEngine.Mathf.Max(0, c.CashGold - Loss);
            }
        }

        _repo.Save(run);
    }

    // UI용(프로그레스바 0~1)
    public static float GetProgress01(Car c)
        => c == null ? 0f : UnityEngine.Mathf.Clamp01(c.CashDecayElapsed / Interval);
}