using ParkingCat.Entities;
using TMPro;
using ParkingCat.ValueObjects;
using UnityEngine;

namespace ParkingCat.Game.TrafficLight
{
    public sealed class TrafficLightView : MonoBehaviour
    {
        [Header("Body")]
        [SerializeField]
        SpriteRenderer bodyRenderer;
        [SerializeField] Sprite bodyRedSprite;
        [SerializeField] Sprite bodyGreenSprite;
        
        [Header("Lamp")]
        [SerializeField]
        Transform lampRoot;
        [SerializeField] SpriteRenderer lampRenderer;
        [SerializeField] Sprite redSprite;
        [SerializeField] Sprite greenSprite;

        [Header("UI")]
        [SerializeField]
        TMP_Text countText;
        
        [Header("Stop Collider")]
        [SerializeField]
        Collider2D stopCollider;

        // 램프가 베이스에서 얼마나 떨어질지(길 옆 연출용)
        [SerializeField] float lampOffset = 1f;

        public void SetWorldPos(Vector2 pos)
        {
            var tr = transform;
            tr.position = new Vector3(pos.x, pos.y, tr.position.z);
        }

        public void SetLamp(TrafficLightState state)
        {
            var lamp = (state == TrafficLightState.Red) ? redSprite : greenSprite;
            if (lampRenderer != null) lampRenderer.sprite = lamp;

            var body = (state == TrafficLightState.Red) ? bodyRedSprite : bodyGreenSprite;
            if (bodyRenderer != null) bodyRenderer.sprite = body;
            
        }
        
        
        public void SetStopColliderEnabled(bool enabled)
        {
            if (stopCollider != null) stopCollider.enabled = enabled;
        }

        public void SetDisplayText(string text)
        {
            if (countText == null) return;
            countText.gameObject.SetActive(true);
            countText.text = text;
        }
        /// <summary>
        /// 판정선 방향에 맞춰 램프 위치/회전만 조정 (라인렌더러 없음)
        /// </summary>
        public void SetDirection(Dir4 dir)
        {
            if (lampRoot == null) return;

            Vector2 offset;
            float rotZ;

            switch (dir)
            {
                case Dir4.Right:
                    offset = Vector2.right;
                    rotZ = 0f;
                    break;
                case Dir4.Left:
                    offset = Vector2.left;
                    rotZ = 180f;
                    break;
                case Dir4.Up:
                    offset = Vector2.up;
                    rotZ = 90f;
                    break;
                case Dir4.Down:
                    offset = Vector2.down;
                    rotZ = -90f;
                    break;
                default:
                    offset = Vector2.right;
                    rotZ = 0f;
                    break;
            }

            lampRoot.localPosition = new Vector3(
                offset.x * lampOffset,
                offset.y * lampOffset,
                lampRoot.localPosition.z
            );

           // lampRoot.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }
    }
}
