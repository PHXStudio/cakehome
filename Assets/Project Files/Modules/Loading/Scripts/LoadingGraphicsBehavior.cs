using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Watermelon
{
    public class LoadingGraphicsBehavior : MonoBehaviour, ILoadingGraphics
    {
        [SerializeField] Camera loadingCamera;
        [SerializeField] CanvasScaler canvasScaler;
        [SerializeField] CanvasGroup canvasGroup;

        [Space]
        [SerializeField] Image backgroundImage;
        [SerializeField] Image loadingImage;
        [SerializeField] TextMeshProUGUI loadingMessageText;
        [SerializeField] TextMeshProUGUI loadingPercentageText;
        [SerializeField] GameObject loadingbarObject;
        [SerializeField] Button retryButton;

        private GameLoading loadingController;
        private AspectRatioFitter backgroundAspectRatioFitter;

        public void Init(GameLoading loadingController)
        {
            this.loadingController = loadingController;

            canvasScaler.MatchSize();
            SetupBackgroundAspectRatio();

            retryButton.onClick.AddListener(OnRetryButtonClicked);
            retryButton.gameObject.SetActive(false);

            loadingbarObject.SetActive(true);

            SetLoadingState(0.0f, "Loading..");
        }

        private void SetupBackgroundAspectRatio()
        {
            if (backgroundImage.sprite == null)
                return;

            backgroundImage.preserveAspect = false;

            backgroundAspectRatioFitter = backgroundImage.GetComponent<AspectRatioFitter>();
            if (backgroundAspectRatioFitter == null)
                backgroundAspectRatioFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();

            backgroundAspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundAspectRatioFitter.aspectRatio = backgroundImage.sprite.rect.width / backgroundImage.sprite.rect.height;
        }

        public void ShowErrorMessage(string message)
        {
            loadingbarObject.SetActive(false);
            retryButton.gameObject.SetActive(true);

            loadingMessageText.text = message;
        }

        public void HideErrorMessage()
        {
            loadingbarObject.SetActive(true);
            retryButton.gameObject.SetActive(false);

            loadingMessageText.text = "Loading..";
        }

        private void OnRetryButtonClicked()
        {
            loadingController.RetryConnection();
        }

        public void SetLoadingState(float state, string message)
        {
            loadingImage.fillAmount = state;
            loadingPercentageText.text = string.Format("{0}%", (state * 100).ToString("0"));
            loadingMessageText.text = message;
        }

        public void OnLoadingFinished()
        {
            canvasGroup.DOFade(0.0f, 0.6f, unscaledTime: true).OnComplete(delegate
            {
                loadingController.OnLoadingFinished();
            });
        }
    }
}
