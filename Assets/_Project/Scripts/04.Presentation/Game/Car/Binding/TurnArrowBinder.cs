using System.Collections.Generic;
using AES.Tools;
using AES.Tools.TBC.Collections;
using ParkingCat.Game.Car.Data;
using ParkingCat.ValueObjects;
using UnityEngine;


namespace ParkingCat.Game.Car.Binding
{
    public enum TurnShape
    {
        Straight, Left, Right,
        UTurnLeft, UTurnRight,
        LeftRight, RightLeft
    }
    public sealed class TurnArrowBinder : ContextListenerBase
    {
        [Header("Arrow")]
        [SerializeField]
        Transform arrowTf;
        [SerializeField] SpriteRenderer arrowRenderer;
        [SerializeField] TurnArrowSpriteSet sprites;


        public Vector2 anchorUp = Vector2.up * 0.25f ;
        public Vector2 anchorRight = Vector2.up * .15f + Vector2.left * .25f;
        public Vector2 anchorDown = Vector2.up * .15f;
        public Vector2 anchorLeft = Vector2.up * .15f + Vector2.right * .25f;
        
        // Dictionary<Dir4, Vector2> _anchors = new()
        // {
        //     { Dir4.Up, Vector2.up * .25f  },
        //     { Dir4.Right, Vector2.up * .15f  +Vector2.left * .25f},
        //     { Dir4.Down, Vector2.up * .15f   },
        //     { Dir4.Left,  Vector2.up * .15f  +Vector2.right * .25f }
        // };

        CompositeDisposable _d = new();

        protected override void OnContextAvailable(IBindingContext ctx)
        {

            if (ctx.GetValue() is not CarItemViewModel vm)
                return;

            // SnapArrowToAnchor();

            // TurnProgram 최초 유효값 1회만 사용
            vm.TurnProgram.Subscribe(ApplyTurnSpec).AddTo(_d);
        }

        protected override void OnContextUnavailable()
        {
            _d.Dispose();
            _d = new();
        }

        private void LateUpdate()
        {
            SnapArrowToAnchor();
        }

        private void SnapArrowToAnchor()
        {
            if (arrowTf == null) return;
        
            var dir = TransformUpToDir4(transform.up);
            var anchor = dir switch
            {
                Dir4.Up    => anchorUp,
                Dir4.Right => anchorRight,
                Dir4.Down  => anchorDown,
                Dir4.Left  => anchorLeft,
                _ => anchorUp
            };

           
            //if (!_anchors.TryGetValue(dir, out var anchor)) return;
        
            arrowTf.localPosition = anchor;
        }
        
        private static Dir4 TransformUpToDir4(Vector2 up)
        {
            float a = Mathf.Atan2(up.x, up.y) * Mathf.Rad2Deg;
        
            if (a >= -45f && a < 45f)    return Dir4.Up;
            if (a >= 45f  && a < 135f)   return Dir4.Right;
            if (a >= -135f && a < -45f)  return Dir4.Left;
            return Dir4.Down;
        }

     
        void ApplyTurnSpec(ValueObjects.TurnProgram spec)
        {
            if (arrowRenderer == null || sprites == null) return;

            var shape = ClassifyTurnShape(spec);

            arrowRenderer.sprite = shape switch
            {
                TurnShape.Straight => sprites.straight,
                TurnShape.Left => sprites.left,
                TurnShape.Right => sprites.right,
                TurnShape.UTurnLeft => sprites.uturnLeft,
                TurnShape.UTurnRight => sprites.uturnRight,
                TurnShape.LeftRight => sprites.leftRight,
                TurnShape.RightLeft => sprites.rightLeft,
                _ => sprites.straight
            };

            arrowRenderer.enabled = (arrowRenderer.sprite != null);
        }

    

        static TurnShape ClassifyTurnShape(ValueObjects.TurnProgram spec)
        {
            if (spec?.Actions == null || spec.Actions.Length == 0)
                return TurnShape.Straight;

            TurnAction? t1 = null;
            TurnAction? t2 = null;

            foreach (var a in spec.Actions)
            {
                if (a == TurnAction.Straight) continue;

                if (t1 == null) t1 = a;
                else
                {
                    t2 = a;
                    break;
                }
            }

            if (t1 == null) return TurnShape.Straight;

            if (t2 == null)
                return t1 == TurnAction.Left ? TurnShape.Left : TurnShape.Right;

            if (t2 == t1)
                return t1 == TurnAction.Left ? TurnShape.UTurnLeft : TurnShape.UTurnRight;

            // 지그재그는 일단 첫 회전으로
            return t1 == TurnAction.Left ? TurnShape.LeftRight : TurnShape.RightLeft;
        }
    }
}