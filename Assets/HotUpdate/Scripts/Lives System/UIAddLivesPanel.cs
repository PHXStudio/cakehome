using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIAddLivesPanel : UIPage, IPopupWindow
    {
        [SerializeField] RectTransform panel;
        [SerializeField] Vector3 hidePos;

        [SerializeField] Image backgroundImage;

        [SerializeField] Button button;
        [SerializeField] Button closeButton;

        [Space]
        [SerializeField] GameObject timerGameObject;
        [SerializeField] GameObject fullGameObject;
        [SerializeField] TMP_Text livesAmountText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] AudioClip lifeRecievedAudio;

        private Vector3 showPos;
        private Color backColor;

        public bool IsOpened => canvas.enabled;

        private SimpleBoolCallback panelClosed;

        private void OnEnable()
        {
            LivesSystem.StatusChanged += OnStatusChanged;
        }

        private void OnDisable()
        {
            LivesSystem.StatusChanged -= OnStatusChanged;
        }

        public override void Init()
        {
            backColor = backgroundImage.color;
            showPos = panel.anchoredPosition;

            button.onClick.AddListener(OnButtonClick);
            closeButton.onClick.AddListener(OnCloseButtonClicked);

            OnStatusChanged(LivesSystem.Status);

            panelClosed = null;
        }

        protected override void OnShow()
        {
            backgroundImage.color = Color.clear;
            backgroundImage.DOColor(backColor, 0.3f);

            panel.anchoredPosition = hidePos;
            panel.DOAnchoredPosition(showPos, 0.3f).SetEasing(Ease.Type.SineOut);

            NotifyOpened();
        }

        protected override void OnHide()
        {
            backgroundImage.DOColor(Color.clear, 0.3f);
            panel.DOAnchoredPosition(hidePos, 0.3f).SetEasing(Ease.Type.SineIn).OnComplete(() =>
            {
                NotifyClosed();
            });
        }

        private void OnStatusChanged(LivesStatus status)
        {
            livesAmountText.text = status.LivesCount.ToString();

            if (status.NewLifeTimerEnabled)
            {
                timerGameObject.SetActive(true);
                fullGameObject.SetActive(false);

                timeText.text = LivesSystem.GetFormatedTime(status.NewLifeTime);
            }
            else
            {
                timerGameObject.SetActive(false);
                fullGameObject.SetActive(true);
            }
        }

        public void OnCloseButtonClicked()
        {
            UIController.HidePage<UIAddLivesPanel>();

            panelClosed?.Invoke(false);
        }

        public void OnButtonClick()
        {
            CustomAnalytics.TrackRewardVideo("add_lives");

            AdsManager.ShowRewardBasedVideo(success =>
            {
                UIController.HidePage<UIAddLivesPanel>();

                if (success)
                {
                    DailyTaskController.AddProgress(DailyTaskType.RewardedVideos);
                    LivesSystem.AddLife(1, true);

                    if (lifeRecievedAudio != null)
                        AudioController.PlaySound(lifeRecievedAudio);

                    panelClosed?.Invoke(true);
                }
            });
        }

        public static void Show(SimpleBoolCallback onPanelClosed = null)
        {
            UIAddLivesPanel addLivesPanel = UIController.GetPage<UIAddLivesPanel>();
            if(addLivesPanel != null)
            {
                addLivesPanel.panelClosed = onPanelClosed;

                UIController.ShowPage<UIAddLivesPanel>();
            }
            else
            {
                onPanelClosed?.Invoke(false);
            }
        }

        public static bool Exists()
        {
            return UIController.GetPage<UIAddLivesPanel>();
        }
    }
}