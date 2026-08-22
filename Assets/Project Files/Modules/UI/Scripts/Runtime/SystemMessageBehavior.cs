using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class SystemMessageBehavior : MonoBehaviour, ISystemMessage
    {
        [Header("Messages")]
        [SerializeField] RectTransform messagePanelRectTransform;
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Loading")]
        [SerializeField] GameObject loadingPanelObject;
        [SerializeField] TextMeshProUGUI loadingStatusText;
        [SerializeField] RectTransform loadingIconRectTransform;

        private TweenCase animationTweenCase;
        private CanvasGroup messagePanelCanvasGroup;
        private bool isLoadingActive;

        private void Awake()
        {
            // 默认隐藏,防止初始化时序问题导致面板常驻
            if (loadingPanelObject != null)
                loadingPanelObject.SetActive(false);
            if (messagePanelRectTransform != null)
                messagePanelRectTransform.gameObject.SetActive(false);
        }

        void ISystemMessage.Init()
        {
            CanvasScaler canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.MatchSize();

            messagePanelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            messageText.AddEvent(EventTriggerType.PointerClick, (data) => OnPanelClick());

            loadingPanelObject.SetActive(false);
            messagePanelRectTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isLoadingActive)
            {
                loadingIconRectTransform.Rotate(0, 0, -50 * Time.unscaledDeltaTime);
            }
        }

        private void OnPanelClick()
        {
            animationTweenCase.KillActive();
            animationTweenCase = messagePanelCanvasGroup.DOFade(0, 0.3f, unscaledTime: true)
                .SetEasing(Ease.Type.CircOut)
                .OnComplete(() => messagePanelRectTransform.gameObject.SetActive(false));
        }

        void ISystemMessage.ShowMessage(string message, float duration)
        {
            if (isLoadingActive) return;

            animationTweenCase.KillActive();

            messageText.text = message;
            messagePanelRectTransform.gameObject.SetActive(true);
            messagePanelCanvasGroup.alpha = 1.0f;

            animationTweenCase = Tween.DelayedCall(duration, () =>
            {
                animationTweenCase = messagePanelCanvasGroup.DOFade(0, 0.5f, unscaledTime: true)
                    .SetEasing(Ease.Type.CircOut)
                    .OnComplete(() => messagePanelRectTransform.gameObject.SetActive(false));
            }, unscaledTime: true);
        }

        void ISystemMessage.ShowLoadingPanel()
        {
            if (isLoadingActive) return;

            animationTweenCase.KillActive();
            messagePanelRectTransform.gameObject.SetActive(false);

            isLoadingActive = true;
            loadingPanelObject.SetActive(true);
        }

        void ISystemMessage.ChangeLoadingMessage(string message)
        {
            loadingStatusText.text = message;
        }

        void ISystemMessage.HideLoadingPanel()
        {
            isLoadingActive = false;
            loadingPanelObject.SetActive(false);
        }
    }
}
