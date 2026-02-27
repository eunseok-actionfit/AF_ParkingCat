using DG.Tweening;
using UnityEngine;


namespace ParkingCat.Game.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class BoardCameraController : MonoBehaviour
    {
        public enum FitMode
        {
            Contain, // 전체가 보이게 (짧은쪽 여백 가능)
            Cover    // 화면을 꽉 채움 (긴쪽 잘림)
        }

        [Header("Ref")]
        [SerializeField]
        Transform target;
        [SerializeField] UnityEngine.Camera targetCamera;

        [Header("Fit Mode")]
        [SerializeField]
        FitMode fitMode = FitMode.Cover;

        [Header("Padding (World Units)")]
        [SerializeField]
        float paddingX = 0f;
        [SerializeField] float paddingY = 0f;

        [Header("Offset (World Units)")]
        [SerializeField]
        Vector2 positionOffset = Vector2.zero;

        [Header("Zoom Limit")]
        [SerializeField]
        float minOrthographicSize = 0.01f;
        [SerializeField] float maxOrthographicSize = 20f;

        [Header("Smoothing")]
        [SerializeField]
        bool smooth = true;
        [SerializeField] float moveDuration = 0.35f;
        [SerializeField] Ease moveEase = Ease.OutQuad;
        [SerializeField] Ease zoomEase = Ease.OutQuad;

        [Header("Auto Refit")]
        [SerializeField]
        bool refitOnResolutionChange = true;

        [Header("Watch Mode (Frames)")]
        [Tooltip("Fit 신호 들어온 뒤 몇 프레임 동안 감시할지")]
        [SerializeField]
        int watchFrames = 5;

        [Tooltip("몇 프레임 간격으로 DoFitToBoard를 호출할지 (1이면 매 프레임)")]
        [SerializeField]
        int watchFrameInterval = 1;

        [Header("Bounds Filter")]
        [SerializeField]
        LayerMask boundsLayerMask = ~0; // 기본: 전부 허용

        int _lastScreenWidth;
        int _lastScreenHeight;

        Tween _posTween;
        Tween _sizeTween;

        // 감시용 상태
        int _watchRemainingFrames;
        bool _pendingInstant;

        bool _isAlive;
        public float MinOrthographicSize => minOrthographicSize;
        public float MaxOrthographicSize => maxOrthographicSize;

        void Reset()
        {
            targetCamera = GetComponent<UnityEngine.Camera>();
        }

        void Awake()
        {
            _isAlive = true;

            if (!targetCamera)
                targetCamera = GetComponent<UnityEngine.Camera>();

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        void OnDisable()
        {
            _isAlive = false;
            KillTweens();
            
        }

        void OnDestroy()
        {
            _isAlive = false;
            KillTweens();
        }

        void Update()
        {
            if (!_isAlive)
                return;

            if (refitOnResolutionChange)
            {
                if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
                {
                    _lastScreenWidth = Screen.width;
                    _lastScreenHeight = Screen.height;

                    FitToBoard(instant: false);
                }
            }
        }

        void LateUpdate()
        {
            if (!_isAlive)
                return;

            if (_watchRemainingFrames <= 0)
                return;

            if ((_watchRemainingFrames % Mathf.Max(1, watchFrameInterval)) == 0)
            {
                bool instant = _pendingInstant;
                DoFitToBoard(instant);
            }

            _watchRemainingFrames--;
            if (_watchRemainingFrames <= 0)
            {
                _pendingInstant = false;
            }
        }
        
        public void FitToBoard(bool instant = false)
        {
            RequestFit(instant);
        }

        void RequestFit(bool instant)
        {
            // 비활성 상태면 요청 무시
            if (!_isAlive || !isActiveAndEnabled)
                return;

            // 한 프레임 안에 여러 번 신호가 들어오면 instant가 한 번이라도 true면 즉시 모드 허용
            if (instant)
                _pendingInstant = true;

            // 새로 신호 들어올 때마다 감시 프레임 수 갱신
            _watchRemainingFrames = Mathf.Max(_watchRemainingFrames, watchFrames);
        }

        void DoFitToBoard(bool instant)
        {
            if (!_isAlive)
                return;

            if (targetCamera == null || target == null)
                return;

            if (!TryGetBoardBounds(out var bounds))
                return;

            var center = bounds.center + (Vector3)positionOffset;
            var currentPos = targetCamera.transform.position;
            var targetPos = new Vector3(center.x, center.y, currentPos.z);

            float halfWidth = bounds.size.x * 0.5f + paddingX;
            float halfHeight = bounds.size.y * 0.5f + paddingY;

            float aspect = targetCamera.aspect;
            float sizeByHeight = halfHeight; // 세로 기준
            float sizeByWidth = halfWidth / Mathf.Max(aspect, 0.0001f); // 가로 기준

            // Contain: 전체가 보이게(여백 가능) -> Max
            // Cover: 화면을 꽉 채움(긴쪽 잘림) -> Min
            float targetSize = fitMode == FitMode.Contain
                ? Mathf.Max(sizeByHeight, sizeByWidth)
                : Mathf.Min(sizeByHeight, sizeByWidth);

            targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);

            if (!smooth || instant || moveDuration <= 0f)
            {
                KillTweens();
                targetCamera.transform.position = targetPos;
                targetCamera.orthographicSize = targetSize;
                return;
            }

            KillTweens();

            _posTween = targetCamera.transform
                .DOMove(targetPos, moveDuration)
                .SetEase(moveEase)
                .SetLink(gameObject); // GO 파괴 시 자동 Kill

            _sizeTween = DOTween.To(
                    () => targetCamera.orthographicSize,
                    v => targetCamera.orthographicSize = v,
                    targetSize,
                    moveDuration)
                .SetEase(zoomEase)
                .SetLink(gameObject);
        }

        bool TryGetBoardBounds(out Bounds bounds)
        {
            bounds = default;

            if (target == null)
                return false;

            var colliders = target.GetComponentsInChildren<Collider2D>(false);
            if (colliders == null || colliders.Length == 0)
                return false;

            bool first = true;

            foreach (var col in colliders)
            {
                if (col == null)
                    continue;

                // 레이어 마스크 필터
                int layer = col.gameObject.layer;
                if ((boundsLayerMask.value & (1 << layer)) == 0)
                    continue;

                var b = col.bounds;

                if (first)
                {
                    bounds = b;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(b);
                }
            }

            // 하나도 안 들어갔으면 false
            return !first;
        }

        void KillTweens()
        {
            if (_posTween != null)
            {
                _posTween.Kill();
                _posTween = null;
            }

            if (_sizeTween != null)
            {
                _sizeTween.Kill();
                _sizeTween = null;
            }
        }
    }
}
