using Cysharp.Threading.Tasks;
using UnityEngine;


namespace ParkingCat.Game.Car
{
    /// <summary>
    /// 충돌 피격 연출을 재생한다.<br/>
    /// 방향성 킥과 흔들림을 결합해 반응을 만든다.
    /// </summary>
    /// <remarks>
    /// `strength`는 0..1 범위를 전제로 한다.<br/>
    /// `target`이 없으면 현재 Transform을 대상으로 사용한다.
    /// </remarks>
    public sealed class HitFx : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("연출을 적용할 대상 트랜스폼")]
        Transform target;

        [SerializeField]
        [Tooltip("연출 지속 시간(초)")]
        float duration = 0.14f;

        [SerializeField]
        [Tooltip("최대 킥 이동 거리")]
        float kickDistance = 0.07f;

        [SerializeField]
        [Tooltip("최대 흔들림 크기")]
        float shakeMagnitude = 0.04f;

        /// <summary>
        /// 충돌 방향과 강도를 기반으로 피격 연출을 재생한다.<br/>
        /// 재생이 끝나면 위치를 원복한다.
        /// </summary>
        /// <param name="impactDir">피격 방향 벡터</param>
        /// <param name="strength">피격 강도(0..1)</param>
        /// <returns>재생 작업</returns>
        public UniTask PlayAsync(Vector2 impactDir, float strength)
        {
            return UniTask.Create(async () =>
            {
                if (target == null) target = transform;

                var basePos = target.localPosition;
                var dir = impactDir.sqrMagnitude > 1e-6f ? impactDir.normalized : Vector2.up;

                float t = 0f;
                float k01 = Mathf.Clamp01(strength);

                float kick = kickDistance * k01;
                float shake = shakeMagnitude * k01;

                while (t < duration)
                {
                    t += UnityEngine.Time.deltaTime;
                    float k = 1f - Mathf.Clamp01(t / duration);

                    float sx = (Mathf.PerlinNoise(t * 80f, 0.1f) - 0.5f) * 2f;
                    float sy = (Mathf.PerlinNoise(0.1f, t * 80f) - 0.5f) * 2f;

                    var offset = (-dir * (kick * k)) + new Vector2(sx, sy) * (shake * k);
                    target.localPosition = basePos + (Vector3)offset;

                    await UniTask.Yield();
                }

                target.localPosition = basePos;
            });
        }
    }
}
