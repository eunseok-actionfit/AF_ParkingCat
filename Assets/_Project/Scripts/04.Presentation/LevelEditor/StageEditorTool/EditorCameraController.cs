using UnityEngine;


namespace ParkingCat.LevelEditor.StageEditorTool
{
    /// <summary>
    /// Editor-only camera controller.
    /// - FitToBoard (manual: e.g. F key)
    /// - Zoom at cursor (wheel)
    /// - Pan (middle-mouse drag)
    ///
    /// Pan 흔들림 방지:
    /// - ScreenToWorldPoint(카메라가 움직인 상태) 기반 delta 대신
    /// - "스크린 델타 -> 월드 델타"로 변환해서 적용
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class EditorCameraController : MonoBehaviour
    {
        [Header("Ref")]
        [SerializeField]
        Transform boundsRoot;
        [SerializeField] UnityEngine.Camera targetCamera;

        [Header("Padding (World Units)")]
        [SerializeField]
        float paddingX = 0f;
        [SerializeField] float paddingY = 0f;

        [Header("Offset (World Units)")]
        [SerializeField]
        Vector2 positionOffset = Vector2.zero;

        [Header("Zoom Limit")]
        [SerializeField]
        float minOrthographicSize = 0.5f;
        [SerializeField] float maxOrthographicSize = 50f;

        [Header("Zoom (Wheel)")]
        [Tooltip("Wheel up -> zoom in (size *= factor). Typical: 0.9")]
        [SerializeField]
        float wheelZoomFactor = 0.9f;

        [Header("Pan (Middle Drag)")]
        [SerializeField]
        float panMultiplier = 1f;

        [Header("Bounds Filter")]
        [SerializeField]
        LayerMask boundsLayerMask = ~0;

        public float MinOrthographicSize => minOrthographicSize;
        public float MaxOrthographicSize => maxOrthographicSize;

        bool _panning;

        // Pan state (screen-delta based)
        Vector3 _camStartPos;
        Vector3 _panStartScreen;

        void Reset()
        {
            targetCamera = GetComponent<UnityEngine.Camera>();
        }

        void Awake()
        {
            if (!targetCamera)
                targetCamera = GetComponent<UnityEngine.Camera>();
        }

        public void FitToBoard(bool instant = true)
        {
            if (!targetCamera || !boundsRoot) return;
            if (!TryGetBounds(out var bounds)) return;

            var center = bounds.center + (Vector3)positionOffset;
            var curPos = targetCamera.transform.position;
            var targetPos = new Vector3(center.x, center.y, curPos.z);

            float halfWidth = bounds.size.x * 0.5f + paddingX;
            float halfHeight = bounds.size.y * 0.5f + paddingY;

            float aspect = targetCamera.aspect;
            float sizeByHeight = halfHeight;
            float sizeByWidth = halfWidth / Mathf.Max(aspect, 0.0001f);

            // Contain
            float targetSize = Mathf.Max(sizeByHeight, sizeByWidth);
            targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);

            targetCamera.transform.position = targetPos;
            targetCamera.orthographicSize = targetSize;
        }

        /// <summary>
        /// Wheel zoom with cursor anchored (screenPos 기준).
        /// wheelDeltaY: Input.mouseScrollDelta.y 그대로 넣으면 됨.
        /// </summary>
        public void ZoomAtScreenPoint(float wheelDeltaY, Vector3 screenPos)
        {
            if (!targetCamera) return;
            if (Mathf.Abs(wheelDeltaY) < 0.01f) return;

            Vector3 before = targetCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, targetCamera.nearClipPlane)
            );

            float factor = Mathf.Clamp(wheelZoomFactor, 0.01f, 0.9999f);
            float size = targetCamera.orthographicSize;

            // wheel up(+) -> zoom in -> size down
            size = wheelDeltaY > 0 ? size * factor : size / factor;
            size = Mathf.Clamp(size, minOrthographicSize, maxOrthographicSize);
            targetCamera.orthographicSize = size;

            Vector3 after = targetCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, targetCamera.nearClipPlane)
            );

            targetCamera.transform.position += (before - after);
        }

        public void BeginPan(Vector3 screenPos)
        {
            if (!targetCamera) return;

            _panning = true;
            _camStartPos = targetCamera.transform.position;
            _panStartScreen = screenPos;
        }

        public void PanTo(Vector3 screenPos)
        {
            if (!_panning || !targetCamera) return;

            Vector3 screenDelta = screenPos - _panStartScreen;

            // Orthographic: 세로 픽셀 1당 월드 유닛
            float unitsPerPixelY = (targetCamera.orthographicSize * 2f) / Mathf.Max(Screen.height, 1);
            float unitsPerPixelX = unitsPerPixelY * targetCamera.aspect;

            // 마우스를 오른쪽/위로 움직이면 카메라는 반대로 움직여야 "잡고 끄는" 느낌
            Vector3 worldDelta = new Vector3(
                -screenDelta.x * unitsPerPixelX,
                -screenDelta.y * unitsPerPixelY,
                0f
            );

            targetCamera.transform.position = _camStartPos + worldDelta * panMultiplier;
        }

        public void EndPan()
        {
            _panning = false;
        }

        public bool IsPanning => _panning;

        bool TryGetBounds(out Bounds bounds)
        {
            bounds = default;
            if (!boundsRoot) return false;

            var colliders = boundsRoot.GetComponentsInChildren<Collider2D>(false);
            if (colliders == null || colliders.Length == 0) return false;

            bool first = true;

            foreach (var col in colliders)
            {
                if (!col) continue;

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

            return !first;
        }
    }
}
