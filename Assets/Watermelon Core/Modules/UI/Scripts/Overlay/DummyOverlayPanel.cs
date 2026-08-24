using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Fallback overlay panel created at runtime when no custom <see cref="IOverlayPanel"/> is found.
    /// Renders a black fullscreen <see cref="UnityEngine.UI.Image"/> and fades it using a coroutine
    /// with <see cref="Time.unscaledDeltaTime"/> so it works correctly while the game is paused.
    /// </summary>
    public class DummyOverlayPanel : BaseOverlayPanel
    {
        private Image image;
        private Coroutine fadeCoroutine;

        public override void Init()
        {
            GameObject overlayObject = new GameObject("Overlay Image");
            overlayObject.transform.SetParent(canvas.transform);
            overlayObject.transform.ResetLocal();

            RectTransform overlayRectTransform = overlayObject.AddComponent<RectTransform>();
            overlayRectTransform.anchorMin = new Vector2(0, 0);
            overlayRectTransform.anchorMax = new Vector2(1, 1);
            overlayRectTransform.sizeDelta = Vector2.zero;

            image = overlayObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0);
            image.raycastTarget = true;
        }

        public override void Show(float duration, SimpleCallback onCompleted)
        {
            StopFade();
            fadeCoroutine = StartCoroutine(FadeCoroutine(1.0f, duration, onCompleted));
        }

        public override void Hide(float duration, SimpleCallback onCompleted)
        {
            StopFade();
            fadeCoroutine = StartCoroutine(FadeCoroutine(0.0f, duration, onCompleted));
        }

        public override void Clear()
        {
            StopFade();

            Object.Destroy(gameObject);
        }

        private void StopFade()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private IEnumerator FadeCoroutine(float targetAlpha, float duration, SimpleCallback onCompleted)
        {
            Color color = image.color;
            float startAlpha = color.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
                image.color = color;
                yield return null;
            }

            color.a = targetAlpha;
            image.color = color;
            fadeCoroutine = null;

            onCompleted?.Invoke();
        }
    }
}
