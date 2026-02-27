using System.Collections;
using Cysharp.Threading.Tasks;
using ParkingCat.Sim;
using UnityEngine;

namespace ParkingCat.Game.Car
{
    /// <summary>
    /// 도메인 `Trajectory`를 씬의 Transform에 재생한다.<br/>
    /// 정방향 및 롤백 연출을 동일 경로로 처리한다.
    /// </summary>
    /// <remarks>
    /// 재생 중 다시 호출되면 기존 코루틴을 중지한다.<br/>
    /// `dt`는 궤적 샘플 간격과 동일한 값 사용을 권장한다.
    /// </remarks>
    public sealed class TrajectoryPlayer : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField, Tooltip("궤적을 적용할 대상 트랜스폼")]
        Transform positionTarget;

        [SerializeField] Transform roateTarget;

        [Header("Playback")]
        [SerializeField, Tooltip("샘플 간격(초)")]
        float dt = 1f / 60f;

        [SerializeField, Tooltip("재생 속도 배율")]
        float speed = 1f;

        [Header("Rotation")]
        [SerializeField, Range(0f, 0.6f), Tooltip("회전을 미리 적용할 선행 비율")]
        float turnLead = 0.35f;

        [SerializeField, Tooltip("회전 보간에 사용할 커브")]
        AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Start Blend (prevent snap at departure)")]
        [SerializeField, Tooltip("재생 시작 시 현재 위치->첫 샘플로 블렌딩할 시간(초). 0이면 스냅(기존 동작)")]
        float startBlendSeconds = 0.10f;

        [SerializeField, Tooltip("재생 시작 시 현재 회전->초기 방향으로 블렌딩할지")]
        bool startBlendRotation = true;

        [SerializeField, Tooltip("첫 구간 이동이 너무 작을 때(노이즈) 방향을 heading으로 유지할 거리^2")]
        float minMoveSqrForDir = 0.0004f; // (0.02m)^2 정도. 프로젝트 단위에 맞게 조절

        // 현재 재생 코루틴 핸들
        Coroutine _co;

        public UniTask PlayAsync(Trajectory traj)
            => PlayRangeAsync(traj, 0, (traj?.Count ?? 0) - 1);

        public UniTask PlayRangeAsync(Trajectory traj, int fromIndex, int toIndex)
            => PlayRangeAsyncInternal(traj, fromIndex, toIndex, reverseFacing: false);

        public UniTask PlayAsyncReverse(Trajectory traj)
            => PlayRangeAsyncReverse(traj, 0, (traj?.Count ?? 0) - 1);

        public UniTask PlayRangeAsyncReverse(Trajectory traj, int fromIndex, int toIndex)
            => PlayRangeAsyncInternal(traj, fromIndex, toIndex, reverseFacing: true);

        public void Stop()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }

        UniTask PlayRangeAsyncInternal(Trajectory traj, int fromIndex, int toIndex, bool reverseFacing)
        {
            Stop();

            var tcs = new UniTaskCompletionSource();
            _co = StartCoroutine(CoPlayRange(traj, fromIndex, toIndex, reverseFacing, tcs));
            return tcs.Task;
        }

        IEnumerator CoPlayRange(
            Trajectory traj,
            int fromIndex,
            int toIndex,
            bool reverseFacing,
            UniTaskCompletionSource tcs)
        {
            const float EPS = 1e-6f;

            if (positionTarget == null) positionTarget = transform;
            if (roateTarget == null) roateTarget = transform;

            if (traj == null || traj.Count == 0)
            {
                tcs.TrySetResult();
                yield break;
            }

            fromIndex = Mathf.Clamp(fromIndex, 0, traj.Count - 1);
            toIndex = Mathf.Clamp(toIndex, 0, traj.Count - 1);

            // 단일 포즈면 기존 동작 유지
            if (toIndex <= fromIndex)
            {
                var p0 = traj.Poses[fromIndex];
                positionTarget.position = new Vector3(p0.Pos.x, p0.Pos.y, positionTarget.position.z);

                var d0 = new Vector2(Mathf.Cos(p0.HeadingRad), Mathf.Sin(p0.HeadingRad));
                roateTarget.up = d0;

                tcs.TrySetResult();
                _co = null;
                yield break;
            }

            // ===== Start Blend: "출발 팍 튐" 방지 =====
            {
                var p0 = traj.Poses[fromIndex];
                var p1 = traj.Poses[fromIndex + 1];

                Vector3 startPos = positionTarget.position;
                Vector3 targetPos = new Vector3(p0.Pos.x, p0.Pos.y, startPos.z);

                // 초기 방향은 "첫 구간 이동 벡터"를 우선하되, 너무 짧으면 heading 사용
                Vector2 d01 = new Vector2(p1.Pos.x - p0.Pos.x, p1.Pos.y - p0.Pos.y);
                bool moveBasedInit = d01.sqrMagnitude > Mathf.Max(EPS, minMoveSqrForDir);

                Vector2 initDir = moveBasedInit
                    ? d01.normalized
                    : new Vector2(Mathf.Cos(p0.HeadingRad), Mathf.Sin(p0.HeadingRad));

                if (reverseFacing && moveBasedInit) initDir = -initDir;

                float initAngle = Mathf.Atan2(initDir.y, initDir.x) * Mathf.Rad2Deg - 90f;
                Quaternion startRot = roateTarget.rotation;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, initAngle);

                if (startBlendSeconds > 0f)
                {
                    float tBlend = 0f;
                    while (tBlend < 1f)
                    {
                        tBlend += UnityEngine.Time.deltaTime / Mathf.Max(0.001f, startBlendSeconds);
                        float k = Mathf.Clamp01(tBlend);

                        positionTarget.position = Vector3.Lerp(startPos, targetPos, k);

                        if (startBlendRotation)
                            roateTarget.rotation = Quaternion.Slerp(startRot, targetRot, k);
                        else
                            roateTarget.rotation = targetRot;

                        yield return null;
                    }
                }
                else
                {
                    // 기존: 즉시 스냅
                    positionTarget.position = targetPos;
                    roateTarget.rotation = targetRot;
                }
            }
            // ===== Start Blend end =====

            float acc = 0f;
            int i = fromIndex;

            while (i < toIndex)
            {
                var p0 = traj.Poses[i];
                var p1 = traj.Poses[i + 1];

                Vector3 a = new Vector3(p0.Pos.x, p0.Pos.y, positionTarget.position.z);
                Vector3 b = new Vector3(p1.Pos.x, p1.Pos.y, positionTarget.position.z);

                Vector2 delta01 = b - a;
                bool moveBased0 = delta01.sqrMagnitude > Mathf.Max(EPS, minMoveSqrForDir);

                Vector2 dir0 = moveBased0
                    ? delta01.normalized
                    : new Vector2(Mathf.Cos(p0.HeadingRad), Mathf.Sin(p0.HeadingRad));

                Vector2 dir1 = dir0;
                bool moveBased1 = moveBased0;

                if (i + 2 <= toIndex)
                {
                    var p2 = traj.Poses[i + 2];
                    Vector3 c = new Vector3(p2.Pos.x, p2.Pos.y, positionTarget.position.z);
                    Vector2 d = c - b;

                    if (d.sqrMagnitude > Mathf.Max(EPS, minMoveSqrForDir))
                    {
                        dir1 = d.normalized;
                        moveBased1 = true;
                    }
                }

                acc += UnityEngine.Time.deltaTime * speed;

                while (acc >= dt)
                {
                    acc -= dt;
                    i++;
                    if (i >= toIndex) break;

                    p0 = traj.Poses[i];
                    p1 = traj.Poses[i + 1];

                    a = new Vector3(p0.Pos.x, p0.Pos.y, positionTarget.position.z);
                    b = new Vector3(p1.Pos.x, p1.Pos.y, positionTarget.position.z);

                    delta01 = b - a;
                    moveBased0 = delta01.sqrMagnitude > Mathf.Max(EPS, minMoveSqrForDir);

                    dir0 = moveBased0
                        ? delta01.normalized
                        : new Vector2(Mathf.Cos(p0.HeadingRad), Mathf.Sin(p0.HeadingRad));

                    dir1 = dir0;
                    moveBased1 = moveBased0;

                    if (i + 2 <= toIndex)
                    {
                        var p2 = traj.Poses[i + 2];
                        Vector3 c = new Vector3(p2.Pos.x, p2.Pos.y, positionTarget.position.z);
                        Vector2 d = c - b;

                        if (d.sqrMagnitude > Mathf.Max(EPS, minMoveSqrForDir))
                        {
                            dir1 = d.normalized;
                            moveBased1 = true;
                        }
                    }
                }

                if (i >= toIndex) break;

                float t = Mathf.Clamp01(acc / dt);

                positionTarget.position = Vector3.LerpUnclamped(a, b, t);

                float kRot = Mathf.Clamp01(t + turnLead);
                kRot = turnCurve.Evaluate(kRot);

                Vector2 dir = Vector2.Lerp(dir0, dir1, kRot);
                if (dir.sqrMagnitude > EPS) dir.Normalize();

                if (reverseFacing && (moveBased0 || moveBased1)) dir = -dir;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                roateTarget.rotation = Quaternion.Euler(0f, 0f, angle);

                yield return null;
            }

            var last = traj.Poses[toIndex];
            positionTarget.position = new Vector3(last.Pos.x, last.Pos.y, positionTarget.position.z);

            Vector2 lastDir;
            bool moveBasedLast = false;

            if (toIndex - 1 >= 0)
            {
                var prev = traj.Poses[toIndex - 1];
                Vector2 d = new Vector2(last.Pos.x - prev.Pos.x, last.Pos.y - prev.Pos.y);

                if (d.sqrMagnitude > Mathf.Max(EPS, minMoveSqrForDir))
                {
                    moveBasedLast = true;
                    lastDir = d.normalized;
                }
                else
                {
                    lastDir = new Vector2(Mathf.Cos(last.HeadingRad), Mathf.Sin(last.HeadingRad));
                }
            }
            else
            {
                lastDir = new Vector2(Mathf.Cos(last.HeadingRad), Mathf.Sin(last.HeadingRad));
            }

            if (reverseFacing && moveBasedLast) lastDir = -lastDir;

            float lastAngle = Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg - 90f;
            roateTarget.rotation = Quaternion.Euler(0f, 0f, lastAngle);

            tcs.TrySetResult();
            _co = null;
        }
    }
}
