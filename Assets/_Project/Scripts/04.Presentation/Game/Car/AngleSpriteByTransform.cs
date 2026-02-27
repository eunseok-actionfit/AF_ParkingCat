using ParkingCat.Game.Car.Data;
using UnityEngine;


namespace ParkingCat.Game.Car
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AngleSpriteByTransform : MonoBehaviour
    {
        [SerializeField] CarSpriteSet spriteSet;

        SpriteRenderer sr;
        Sprite last;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if(spriteSet == null)
                return;
            Vector2 dir = transform.up;

            float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            Sprite next = angle switch
            {
                >= -45f and < 45f    => spriteSet.up,
                >= 45f  and < 135f   => spriteSet.right,
                >= -135f and < -45f  => spriteSet.left,
                _                    => spriteSet.down
            };

            // 변경 있을 때만 적용 (불필요한 Set 방지)
            if (next != null && next != last)
            {
                sr.sprite = next;
                last = next;
            }
        }
    }
}