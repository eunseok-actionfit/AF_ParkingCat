using UnityEngine;

public sealed class TutorialArrowFxUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform arrowRect;      // 화살표 UI
    [SerializeField] RectTransform canvasRect;     // 최상위 Canvas의 RectTransform
    [SerializeField] Camera worldCamera;           // 보드 카메라 (Overlay라도 필요)

    [Header("Layout")]
    [SerializeField] Vector3 worldOffset = new Vector3(0, 1.0f, 0);

    [Header("Anim")]
    [SerializeField] float bobAmp = 8f;            // UI 픽셀 단위
    [SerializeField] float bobFreq = 2.5f;

    Transform _targetWorld;
    Vector2 _baseAnchored;

    public void Bind(Transform targetWorld)
    {
        _targetWorld = targetWorld;
        gameObject.SetActive(true);
    }

    public void Unbind()
    {
        _targetWorld = null;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (_targetWorld == null || worldCamera == null) return;

        // 1) 월드 -> 스크린
        Vector3 worldPos = _targetWorld.position + worldOffset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // 카메라 뒤면 숨김
        if (screenPos.z < 0f)
        {
            arrowRect.gameObject.SetActive(false);
            return;
        }
        arrowRect.gameObject.SetActive(true);

        // 2) 스크린 -> 캔버스 로컬
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, null, out var localPoint))
        {
            _baseAnchored = localPoint;
        }

        // 3) Bob(픽셀)
        float bob = Mathf.Sin(Time.time * bobFreq) * bobAmp;
        arrowRect.anchoredPosition = _baseAnchored + new Vector2(0, bob);
    }
}