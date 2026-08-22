
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIGameOver : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;

        [SerializeField] UIScaleAnimation levelFailed;
        [SerializeField] UIFadeAnimation backgroundFade;

        [SerializeField] Button menuButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button reviveButton;

        [SerializeField] UIScaleAnimation menuButtonScalable;
        [SerializeField] UIScaleAnimation replayButtonScalable;
        [SerializeField] UIScaleAnimation reviveButtonScalable;

        [Header("D4 失败挽留")]
        [SerializeField] TMP_Text reviveButtonText;
        [SerializeField] TMP_Text replayButtonText;
        [SerializeField] int reviveCoinCost = 100;

        private TweenCase continuePingPongCase;

        public override void Init()
        {
            // 先清空场景/prefab 残留的序列化事件（如旧 ContinueButton），避免双重触发
            menuButton.onClick.RemoveAllListeners();
            replayButton.onClick.RemoveAllListeners();
            reviveButton.onClick.RemoveAllListeners();

            menuButton.onClick.AddListener(MenuButton);
            replayButton.onClick.AddListener(ReplayButton);
            reviveButton.onClick.AddListener(ReviveButton);

            if (reviveButtonText != null)
                reviveButtonText.text = $"续局 {reviveCoinCost} 积分";
            if (replayButtonText != null)
                replayButtonText.text = "半价重试";

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        #region Show/Hide

        protected override void OnShow()
        {
            levelFailed.Hide(immediately: true);
            menuButtonScalable.Hide(immediately: true);
            replayButtonScalable.Hide(immediately: true);
            reviveButtonScalable.Hide(immediately: true);

            float fadeDuration = 0.3f;
            backgroundFade.Show(fadeDuration);

            Tween.DelayedCall(fadeDuration * 0.8f, delegate
            {
                levelFailed.Show();

                menuButtonScalable.Transform.localScale = Vector3.zero;
                menuButtonScalable.Transform.DOScale(Vector3.one * 1.05f, 0.5f).SetDelay(0.75f).SetEasing(Ease.Type.BackOut);
                menuButtonScalable.Transform.localScale = Vector3.zero;
            menuButtonScalable.Transform.DOScale(Vector3.one * 1.05f, 0.5f).SetDelay(0.75f).SetEasing(Ease.Type.BackOut);
            menuButtonScalable.Transform.localScale = Vector3.zero;
            menuButtonScalable.Transform.DOScale(Vector3.one * 1.05f, 0.5f).SetDelay(0.75f).SetEasing(Ease.Type.BackOut);
            menuButtonScalable.Show(scaleMultiplier: 1.05f, delay: 0.75f);
                replayButtonScalable.Transform.localScale = Vector3.zero;
                replayButtonScalable.Transform.DOScale(Vector3.one * 1.05f, 0.5f).SetDelay(0.75f).SetEasing(Ease.Type.BackOut);
                replayButtonScalable.Transform.localScale = Vector3.zero;
            replayButtonScalable.Transform.DOScale(Vector3.one * 1.05f, 0.5f).SetDelay(0.75f).SetEasing(Ease.Type.BackOut);
            replayButtonScalable.Transform.localScale = Vector3.zero;
            replayButtonScalable.Transform.DOScale(Vector3.one * 1.05f, 0.5f).SetDelay(0.75f).SetEasing(Ease.Type.BackOut);
            replayButtonScalable.Show(scaleMultiplier: 1.05f, delay: 0.75f);
                reviveButtonScalable.Show(scaleMultiplier: 1.05f, delay: 0.25f);

                continuePingPongCase = reviveButtonScalable.Transform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);

                NotifyOpened();
            });

        }

        protected override void OnHide()
        {
            backgroundFade.Hide(immediately: true);

            if (continuePingPongCase != null && continuePingPongCase.IsActive)
                continuePingPongCase.Kill();

            NotifyClosed();
        }

        #endregion

        #region Buttons 

        private void ReviveButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            // D4 积分续局：花费积分直接续命（替代原广告复活）
            if (!GameController.ReviveWithCoins(reviveCoinCost))
            {
                Debug.Log("[GameOver] 积分不足，无法续局");
                UIAddLivesPanel.Show();
                return;
            }

            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIGame>();
        }

        private void ReplayButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            // D4 半价重试：两次半价重试 = 1 体力
            if (LivesSystem.CanStartHalfPrice())
            {
                UIController.HidePage<UIGameOver>();

                GameController.ReplayLevelHalfPrice();
            }
            else
            {
                UIAddLivesPanel.Show();
            }
        }

        private void MenuButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            UIController.HidePage<UIGameOver>(() =>
            {
                GameController.ReturnToMenu();
            });
        }

        #endregion
    }
}