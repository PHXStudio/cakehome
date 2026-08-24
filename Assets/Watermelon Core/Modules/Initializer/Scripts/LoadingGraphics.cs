using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Watermelon
{
    public class LoadingGraphics : MonoBehaviour, ILoadingGraphics
    {
        [SerializeField] TextMeshProUGUI loadingText;
        [SerializeField] Image backgroundImage;
        [SerializeField] CanvasScaler canvasScaler;
        [SerializeField] Camera loadingCamera;

        public void Init(GameLoading gameLoading)
        {
            canvasScaler.MatchSize();

            SetLoadingState(0.0f, "Loading..");
        }

        public void SetLoadingState(float progress, string message)
        {
            if (loadingText != null)
                loadingText.text = message;
        }

        public void ShowErrorMessage(string message)
        {
            loadingText.text = message;
        }

        public void HideErrorMessage() { }

        public void OnLoadingFinished()
        {
            if (loadingText == null || backgroundImage == null)
            {
                Destroy(gameObject);
                return;
            }

            loadingText.DOFade(0.0f, 0.6f, unscaledTime: true);
            backgroundImage.DOFade(0.0f, 0.6f, unscaledTime: true).OnComplete(delegate
            {
                Destroy(gameObject);
            });
        }
    }
}
