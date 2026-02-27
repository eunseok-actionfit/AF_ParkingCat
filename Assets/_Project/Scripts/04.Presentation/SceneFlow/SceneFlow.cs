using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ParkingCat.Game.Args;


namespace ParkingCat.SceneFlow
{
    public sealed class SceneFlow
    {
        // 게임 고정 키
        const string LobbyKey = "Lobby";
        const string InGameKey = "InGame";

        readonly SceneTransitionService _scene;
        readonly SceneTransitionBootstrapConfig _cfg;
        readonly List<string> _keep;
    
        public SceneFlow(SceneTransitionService scene, SceneTransitionBootstrapConfig cfg)
        {
            _scene = scene;
            _cfg = cfg;

            _keep = new List<string>();
            if (_cfg?.KeepSceneNames != null) _keep.AddRange(_cfg.KeepSceneNames);
            if (!_keep.Contains("Persistent")) _keep.Add("Persistent");
        }

        public UniTask GoLobbyAsync(CancellationToken ct= default)
        {
            return _scene.RunAsync(new LoadRequest
            {
                DestinationKey = LobbyKey,
                ShowLoadingScreen = true,
                LoadAdditive = false,

                UnloadPolicy = UnloadPolicy.ActiveScene,
                KeepSceneNames = new List<string>(_keep),

                ActivationGate = GateId.BeforeActivation,
                ActivationGateTimeoutMs = 0,

                EntryFadeDuration = _cfg?.EntryFadeDuration ?? 0.15f,
                ExitFadeDuration  = _cfg?.ExitFadeDuration ?? 0.15f,

                UseAntiSpill = _cfg != null && _cfg.UseAntiSpill,
                EnableFallback = true,
                FallbackDestinationKey = LobbyKey,
            }, ct);
        }

        public UniTask GoInGameAsync(int stageNumber, CancellationToken ct = default)
        {
            return _scene.RunAsync(new LoadRequest
            {
                DestinationKey = InGameKey,
                SceneArgs = new StageEnterArgs { StageNumber = stageNumber },

                ShowLoadingScreen = true,
                LoadAdditive = false,

                UnloadPolicy = UnloadPolicy.ActiveScene,
                KeepSceneNames = new List<string>(_keep),

                ActivationGate = GateId.BeforeActivation,
                ActivationGateTimeoutMs = 0,

                EntryFadeDuration = _cfg?.EntryFadeDuration ?? 0.15f,
                AfterEntryFadeDelay = 0.15f,    
                ExitFadeDuration  = _cfg?.ExitFadeDuration ?? 0.15f,

                UseAntiSpill = _cfg != null && _cfg.UseAntiSpill,
                EnableFallback = true,
                FallbackDestinationKey = LobbyKey,
            }, ct);
        }
    }
}
