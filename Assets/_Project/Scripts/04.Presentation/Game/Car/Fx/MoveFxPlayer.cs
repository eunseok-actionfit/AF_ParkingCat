using System;
using AES.Tools;
using Cysharp.Threading.Tasks;
using ParkingCat.Game.TrafficLight;
using ParkingCat.Move.Ports;
using ParkingCat.Sim;
using ParkingCat.World;
using UnityEngine;

namespace ParkingCat.Game.Car.Fx
{
    public sealed class MoveFxPlayer : IMoveFxPlayer, IDisposable
    {
        readonly IInputGate _gate;
        readonly ICarRuntimeRegistry _runtime;
        readonly IStateWriter _state;

        readonly float _hitStopSeconds = 0.08f;

        int _playingCount = 0;
        public bool IsPlaying => _playingCount > 0;

        public event Action OnAllFxCompleted;

        //EventBinding<TrafficLightPenaltyEvent> _tlPenaltyBinding;

        public MoveFxPlayer(IInputGate gate, ICarRuntimeRegistry runtime, IStateWriter state)
        {
            _gate = gate;
            _runtime = runtime;
            _state = state;

            // _tlPenaltyBinding?.Deregister();
            // _tlPenaltyBinding = new EventBinding<TrafficLightPenaltyEvent>()
            // {
            //     Name = "TrafficLightPenalty->MoveFxPlayer",
            //     Owner = this
            // }.Register(e =>
            // {
            //     //_state.ApplyExtraMoveCost(e.ExtraCost);
            // });
        }

        public void Dispose()
        {
            // _tlPenaltyBinding?.Deregister();
            // _tlPenaltyBinding = null;
        }

        public async UniTask PlayAsync(int moverCarId, SimResult result)
        {
            if (result.Decision == SimDecision.Rejected) return;
            if (!_runtime.TryGet(moverCarId, out var moverInfo)) return;

            var moverView = moverInfo.PosTf.GetComponent<CarView>();
            if (moverView == null) return;

            _playingCount++;

            try
            {
                // ================= Commit =================
                if (result.Decision == SimDecision.Commit)
                {
                    _runtime.Unregister(moverCarId);

                    await moverView.PlayTrajectoryAsync(result.Forward);
                    await moverView.PlayExitFxAndDisableAsync();
                    UnityEngine.Object.Destroy(moverView.gameObject);
                    return;
                }

                // ================= Rollback =================
                _gate.Lock();

                try
                {
                    int hit = result.HitFrameIndex < 0
                        ? result.Forward.Count - 1
                        : result.HitFrameIndex;

                    // Forward(가던 구간) 재생: collider ON 유지 (신호등 패널티는 여기서만 허용)
                    moverView.SetBodyColliderEnabled(true);

                    await moverView.PlayTrajectoryRangeAsync(result.Forward, 0, hit);

                    var dirUnity = new Vector2(result.ImpactDir.x, result.ImpactDir.y);
                    float strength = Mathf.Clamp01(result.ImpactStrength);

                    if (result.VictimCarId >= 0 &&
                        _runtime.TryGet(result.VictimCarId, out var victimInfo))
                    {
                        var victimView = victimInfo.PosTf.GetComponent<CarView>();

                        if (victimView != null)
                        {
                            await UniTask.WhenAll(
                                moverView.PlayHitAsync(dirUnity, strength),
                                victimView.PlayHitAsync(-dirUnity, strength));
                        }
                        else
                        {
                            await moverView.PlayHitAsync(dirUnity, strength);
                        }
                    }
                    else
                    {
                        await moverView.PlayHitAsync(dirUnity, strength);
                    }

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_hitStopSeconds),
                        DelayType.UnscaledDeltaTime);

                    // Reverse(되돌아오는 구간) 재생: collider OFF (트리거/충돌 무시)
                    moverView.SetBodyColliderEnabled(false);
                    try
                    {
                        await moverView.PlayTrajectoryAsyncReverse(result.Rollback);
                    }
                    finally
                    {
                        // 다음 턴/상태를 위해 복구
                        moverView.SetBodyColliderEnabled(true);
                    }
                }
                finally
                {
                    _gate.Unlock();
                }
            }
            finally
            {
                _playingCount--;

                if (_playingCount <= 0)
                {
                    _playingCount = 0;

                    UniTask.Void(async () =>
                    {
                        await UniTask.SwitchToMainThread();
                        OnAllFxCompleted?.Invoke();
                    });
                }
            }
        }
    }
}
