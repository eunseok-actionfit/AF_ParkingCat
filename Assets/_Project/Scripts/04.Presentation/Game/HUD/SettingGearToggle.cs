using AES.Tools.SwitchToggle;
using DG.Tweening;
using UnityEngine;


namespace ParkingCat.Game.HUD
{
    public class SettingGearToggle : SimpleToggle
    {
        [SerializeField] Transform gear;
        [SerializeField] float duration = 0.5f;

        [SerializeField] DimView dimView;

        protected override void Awake()
        {
            base.Awake();

            if (dimView != null)
                dimView.Init();       // 초기화는 여기서

            onTurnOn.AddListener(() => dimView?.FadeIn());
            onTurnOff.AddListener(() => dimView?.FadeOut());

            onTurnOn.AddListener(RotateOn);
            onTurnOff.AddListener(RotateOff);
        }

        void RotateOn()
        {
            gear.DOKill();
            gear.DOLocalRotate(
                new Vector3(0, 0, 360f),
                duration,
                RotateMode.FastBeyond360   // 누적 회전
            ).SetRelative(true).SetEase(Ease.OutCubic);
        }

        void RotateOff()
        {
            gear.DOKill();
            gear.DOLocalRotate(
                new Vector3(0, 0, -360f),
                duration,
                RotateMode.FastBeyond360
            ).SetRelative(true).SetEase(Ease.OutCubic);
        }
    }
}