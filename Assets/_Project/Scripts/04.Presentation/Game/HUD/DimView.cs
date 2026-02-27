using System.Collections;
using UnityEngine;


namespace ParkingCat.Game.HUD
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DimView : MonoBehaviour
    {
        [SerializeField] float fadeDuration = 0.25f;

        CanvasGroup canvasGroup;
        Coroutine fadeRoutine;

        public void Init()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void FadeIn()
        {
            gameObject.SetActive(true);
            StartFade(1f);
        }

        public void FadeOut()
        {
            StartFade(0f);
        }

        public void ShowImmediate()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void HideImmediate()
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        void StartFade(float targetAlpha)
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        IEnumerator FadeRoutine(float targetAlpha)
        {
            float startAlpha = canvasGroup.alpha;
            float time = 0f;

            if (targetAlpha > startAlpha)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = false;
            }

            while (time < fadeDuration)
            {
                time += UnityEngine.Time.deltaTime;
                float t = fadeDuration > 0f ? time / fadeDuration : 1f;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            if (Mathf.Approximately(targetAlpha, 0f))
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
            else
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            fadeRoutine = null;
        }
    }
}
