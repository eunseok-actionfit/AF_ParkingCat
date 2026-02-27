using System;
using AES.Tools;
using AES.Tools.Controllers.Core;
using JSAM;
using UnityEngine;


namespace ParkingCat.ETC
{
    [RequireComponent(typeof(TouchButton))]
    public  class ButtonFeedback : MonoBehaviour
    {
        public enum ImpactScale
        {
            Weak,
            Soft,
            Medium,
            Hard
        }
        
        public ImpactScale impactScale = ImpactScale.Weak;
        public NewAudioLibrarySounds sound = NewAudioLibrarySounds.Pop;
        public bool playSound = true;
        void Awake()
        {
            GetComponent<TouchButton>().ButtonTapped.AddListener(Feedback);
        }

        public void Feedback()
        {
            switch (impactScale)
            {
                case ImpactScale.Weak:
                    Haptic.Weak();
                    break;
                case ImpactScale.Soft:
                    Haptic.Soft();
                    break;
                case ImpactScale.Medium:
                    Haptic.Medium();
                    break;
                case ImpactScale.Hard:
                    Haptic.Hard();
                    break;
            }

            if(playSound)AudioManager.PlaySound(sound);
        }
    }
}


