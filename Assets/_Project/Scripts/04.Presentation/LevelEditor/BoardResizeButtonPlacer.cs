using UnityEngine;
using UnityEngine.Tilemaps;


namespace ParkingCat.LevelEditor
{
    public sealed class BoardResizeButtonPlacer : MonoBehaviour
    {
        [SerializeField] Tilemap tilemap;

        [SerializeField] RectTransform upPlus;
        [SerializeField] RectTransform upMinus;
        [SerializeField] RectTransform downPlus;
        [SerializeField] RectTransform downMinus;
        [SerializeField] RectTransform leftPlus;
        [SerializeField] RectTransform leftMinus;
        [SerializeField] RectTransform rightPlus;
        [SerializeField] RectTransform rightMinus;

        [SerializeField] float offset = 0.6f;

        void LateUpdate()
        {
            Refresh();
        }

        void Refresh()
        {
            if (!tilemap) return;

            tilemap.CompressBounds();

            var boundsInt = tilemap.cellBounds;
            if (boundsInt.size.x <= 0 || boundsInt.size.y <= 0) return;

            Vector3Int min = boundsInt.min;
            Vector3Int size = boundsInt.size;

            Vector3 wMin = tilemap.CellToWorld(min);
            Vector3 wMax = tilemap.CellToWorld(min + size);

            float minX = Mathf.Min(wMin.x, wMax.x);
            float maxX = Mathf.Max(wMin.x, wMax.x);
            float minY = Mathf.Min(wMin.y, wMax.y);
            float maxY = Mathf.Max(wMin.y, wMax.y);

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            Vector3 top    = new(centerX, maxY + offset, 0f);
            Vector3 bottom = new(centerX, minY - offset, 0f);
            Vector3 left   = new(minX - offset, centerY, 0f);
            Vector3 right  = new(maxX + offset, centerY, 0f);

            upPlus.position    = top + Vector3.right * 0.5f;
            upMinus.position   = top + Vector3.left * 0.5f;

            downPlus.position  = bottom + Vector3.right * 0.5f;
            downMinus.position = bottom + Vector3.left * 0.5f;

            leftPlus.position  = left + Vector3.up * 0.5f;
            leftMinus.position = left + Vector3.down * 0.5f;

            rightPlus.position  = right + Vector3.up * 0.5f;
            rightMinus.position = right + Vector3.down * 0.5f;
        }
    }
}
