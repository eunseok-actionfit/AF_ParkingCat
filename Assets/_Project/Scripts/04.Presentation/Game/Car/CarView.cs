using System;
using Cysharp.Threading.Tasks;
using JSAM;
using ParkingCat.Entities;
using ParkingCat.Sim;
using ParkingCat.World;
using UnityEngine;
using VContainer;


namespace ParkingCat.Game.Car
{

    /// <summary>
    /// 씬의 차량 오브젝트를 표현하고 연출을 재생한다.<br/>
    /// 궤적 재생, 피격, 퇴장 효과를 제공한다.
    /// </summary>
    /// <remarks>
    /// `CarId`는 도메인 차량과 매핑된다.<br/>
    /// 레지스트리 등록/해제는 생명주기에 따라 자동 수행된다.
    /// </remarks>
    public sealed class CarView : MonoBehaviour
    {
        // 현재 런타임 상태
        public CarRuntimeState RuntimeState { get; private set; } = CarRuntimeState.Active;

        // 다음 턴 액션을 가리키는 커서
        public int TurnCursor;

        // 차량의 턴 프로그램
        public ValueObjects.TurnProgram TurnProgram = new();

        [SerializeField]
        [Tooltip("충돌 시 피격 연출 컴포넌트")]
        HitFx hitFx;

        [SerializeField]
        [Tooltip("궤적 재생을 담당하는 플레이어")]
        TrajectoryPlayer player;

        [SerializeField] Transform visualRoot;
        
        [SerializeField]
        [Tooltip("차량 본체 충돌 콜라이더")]
        BoxCollider2D bodyCollider;
        
        [SerializeField] GameObject exitHintFx; // 아웃라인/글로우 등
        
        [Header("Force Exit FX")]
        [SerializeField]
        GameObject forceExitFxPrefab;
        [SerializeField] Vector3 forceExitFxLocalOffset = Vector3.zero;
        


        public int CarId { get; private set; }

        ICarRuntimeRegistry _runtime;
        bool _registered;

        [Inject]
        public void Construct(ICarRuntimeRegistry runtime) => _runtime = runtime;

        public void RegisterToRuntime(int carId, ValueObjects.TurnProgram program, int turnCursor)
        {
            // 재사용/리바인딩 대비
            if (_registered && CarId != carId)
                UnregisterFromRuntime();

            CarId = carId;

            if (_runtime == null) return;

            if (!_registered)
            {
                _runtime.Register(
                    carId: CarId,
                    posTf: transform,
                    rotTf: visualRoot != null ? visualRoot : transform,
                    program: program ?? new ValueObjects.TurnProgram(),
                    turnCursor: turnCursor,
                    dims: GetDims());

                _registered = true;
            }
            else
            {
                _runtime.SetTurnState(CarId, program ?? new ValueObjects.TurnProgram(), turnCursor);
            }
        }

        public void UnregisterFromRuntime()
        {
            if (!_registered) return;
            if (_runtime == null) return;

            _runtime.Unregister(CarId);
            _registered = false;
        }

        void OnDisable()
        {
            // 풀링/비활성화 대응: binder가 못 받는 경우도 있으니 안전장치로 유지
            UnregisterFromRuntime();
        }

        void OnDestroy()
        {
            UnregisterFromRuntime();
            AudioManager.StopSound(NewAudioLibrarySounds.Bustouch);
        }

        /// <summary>
        /// 현재 콜라이더 크기와 스케일을 기반으로 차량 치수를 반환한다.
        /// </summary>
        /// <returns>차량 치수</returns>
        public CarDims GetDims()
        {
            Vector2 size = Vector2.Scale(bodyCollider.size, visualRoot.lossyScale);
            return new CarDims(length: size.y, width: size.x);
        }

        /// <summary>
        /// 궤적의 일부 구간을 정방향으로 재생한다.
        /// </summary>
        /// <param name="traj">재생할 궤적</param>
        /// <param name="fromIndex">시작 인덱스</param>
        /// <param name="toIndex">종료 인덱스</param>
        /// <returns>재생 작업</returns>
        public async UniTask PlayTrajectoryRangeAsync(Trajectory traj, int fromIndex, int toIndex)
        {
            AudioManager.PlaySound(NewAudioLibrarySounds.Bustouch);
            await player.PlayRangeAsync(traj, fromIndex, toIndex);
            AudioManager.StopSound(NewAudioLibrarySounds.Bustouch);
        }

        /// <summary>
        /// 궤적 전체를 정방향으로 재생한다.
        /// </summary>
        /// <param name="traj">재생할 궤적</param>
        /// <returns>재생 작업</returns>
        public async UniTask PlayTrajectoryAsync(Trajectory traj)
        {
            AudioManager.PlaySound(NewAudioLibrarySounds.Bustouch);
            await player.PlayAsync(traj);
            AudioManager.StopSound(NewAudioLibrarySounds.Bustouch);
        }

        /// <summary>
        /// 궤적의 일부 구간을 역방향으로 재생한다.<br/>
        /// 진행 방향은 역방향을 기준으로 연출한다.
        /// </summary>
        /// <param name="traj">재생할 궤적</param>
        /// <param name="fromIndex">시작 인덱스</param>
        /// <param name="toIndex">종료 인덱스</param>
        /// <returns>재생 작업</returns>
        public async UniTask PlayTrajectoryRangeAsyncReverse(Trajectory traj, int fromIndex, int toIndex)
        {
            AudioManager.PlaySound(NewAudioLibrarySounds.Bustouch);
            await player.PlayRangeAsyncReverse(traj, fromIndex, toIndex);
            AudioManager.StopSound(NewAudioLibrarySounds.Bustouch);
        }
            

        /// <summary>
        /// 궤적 전체를 역방향으로 재생한다.<br/>
        /// 진행 방향은 역방향을 기준으로 연출한다.
        /// </summary>
        /// <param name="traj">재생할 궤적</param>
        /// <returns>재생 작업</returns>
        public async UniTask PlayTrajectoryAsyncReverse(Trajectory traj)
        {
            AudioManager.PlaySound(NewAudioLibrarySounds.Bustouch);
            await player.PlayAsyncReverse(traj);
            AudioManager.StopSound(NewAudioLibrarySounds.Bustouch);
        }

        /// <summary>
        /// 충돌 피격 연출을 재생한다.<br/>
        /// 연출 컴포넌트가 없으면 즉시 완료된다.
        /// </summary>
        /// <param name="impactDir">피격 방향</param>
        /// <param name="strength">피격 강도</param>
        /// <returns>재생 작업</returns>
        public UniTask PlayHitAsync(Vector2 impactDir, float strength)
        {
            AudioManager.PlaySound(NewAudioLibrarySounds.Crash);
            return hitFx != null ? hitFx.PlayAsync(impactDir, strength) : UniTask.CompletedTask;
        }

        /// <summary>
        /// 퇴장 연출을 재생하고 충돌을 비활성화한다.<br/>
        /// 스프라이트를 전방으로 이동시키며 페이드아웃한다.
        /// </summary>
        /// <param name="slide">전방 이동 거리</param>
        /// <param name="seconds">연출 시간</param>
        /// <returns>재생 작업</returns>
        public async UniTask PlayExitFxAndDisableAsync(float slide = 0.6f, float seconds = 0.18f)
        {
            if (bodyCollider != null) bodyCollider.enabled = false;

            var renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            if (renderers == null || renderers.Length == 0) return;

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + visualRoot.up * slide;

            var baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].color;

            float t = 0f;
            while (t < 1f)
            {
                t += UnityEngine.Time.deltaTime / Mathf.Max(0.001f, seconds);
                float k = Mathf.Clamp01(t);

                transform.position = Vector3.Lerp(startPos, endPos, k);

                float a = 1f - k;
                for (int i = 0; i < renderers.Length; i++)
                {
                    var c = baseColors[i];
                    c.a = a;
                    renderers[i].color = c;
                }

                await UniTask.Yield();
            }

            AudioManager.PlaySound(NewAudioLibrarySounds.Bbusout);
        }
        
        public async UniTask PlayForceExitAsync(
            float carSlide = 0f,
            float carFadeSeconds = 0.5f,
            float fxLifeSeconds = 0.5f)
        {
            // FX는 페이드 없음
            GameObject fx = null;
            if (forceExitFxPrefab != null)
            {
                fx = Instantiate(
                    forceExitFxPrefab,
                    transform.position + forceExitFxLocalOffset, // 월드 오프셋
                    Quaternion.identity);
            }

            // 차량 본체 페이드 + 이동
            await PlayExitFxAndDisableAsync(carSlide, carFadeSeconds);

            // FX 수명 관리
            if (fx != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(fxLifeSeconds));
                Destroy(fx);
            }
            AudioManager.PlaySound(NewAudioLibrarySounds.Bbusout);
        }

        public void SetBodyColliderEnabled(bool on)
        {
            if (bodyCollider != null) bodyCollider.enabled = on;
        }
        
        public void SetExitHint(bool on)
        {
            if (exitHintFx != null) exitHintFx.SetActive(on);
        }

        public void MarkLeaving()
        {
        }
    }
}
