using System;
using UnityEngine;

namespace Watermelon
{
    // One-shot: highlights the pinned spawner-reward task card the first time the player has a
    // pending spawner reward while on the board (UIGame), teaching them to tap it to place the
    // spawner. Parented under TutorialController in Game.unity (auto-discovered).
    public class SpawnerRewardTutorial : BaseTutorial
    {
        [Header("Heroine")]
        [SerializeField] CharacterData heroineCharacter;
        [SerializeField] EmotionType heroineEmotion = EmotionType.Default;

        [Header("Hint")]
        [SerializeField, TextArea] string hintText = "Your rewards will be here. Tap to place them on the board.";

        private UISpawnerRewardCard cachedCard;
        private bool highlightShown;
        private UISpawnerRewardCard pendingCard;
        private TweenCase pendingHighlightTween;

        protected override void OnInitialised() { }

        protected override void OnStarted()
        {
            UIController.PageOpened += OnPageOpened;
            UIController.PageClosed += OnPageClosed;
            if (TaskController.Instance != null)
                TaskController.Instance.SpawnerQueue.OnChanged += TryShow;

            TryShow();
        }

        protected override void OnFinished() => Cleanup();

        protected override void OnUnloaded() => Cleanup();

        private void OnPageOpened(UIPage page, Type pageType) => TryShow();
        private void OnPageClosed(UIPage page, Type pageType) => TryShow();

        // Deferred one frame — mirrors FirstStartTutorial.HighlightMapButton: PageOpened fires
        // before IsPageDisplayed flips, and the spawner card is (re)built off the same
        // SpawnerQueue.OnChanged event this method itself is subscribed to, so the card may not
        // exist yet on the same frame the queue changes.
        private void TryShow()
        {
            Tween.NextFrame(() =>
            {
                if (!IsActive) return;

                UIGame gamePage = UIController.GetPage<UIGame>();
                bool onBoard = gamePage != null && gamePage.IsPageDisplayed;

                if (!onBoard)
                {
                    CancelPendingHighlight();
                    if (highlightShown) HideHighlight();
                    return;
                }

                UISpawnerRewardCard card = UITaskPanel.ActiveSpawnerCard;
                if (card == null)
                {
                    CancelPendingHighlight();
                    if (highlightShown) HideHighlight();
                    return;
                }

                if (highlightShown && cachedCard == card) return;
                if (pendingCard == card) return;

                if (highlightShown) HideHighlight();
                CancelPendingHighlight();

                // The card is still growing in (UISpawnerRewardCard.PlayEnter tweens its
                // LayoutElement's preferredWidth over TaskCardAnimation.EnterDuration) — capturing
                // the spotlight rect right now would encapsulate only its current, still-shrunk
                // width. Wait for the enter animation to settle first.
                pendingCard = card;
                pendingHighlightTween = Tween.DelayedCall(TaskCardAnimation.EnterDuration, () =>
                {
                    pendingHighlightTween = null;
                    if (pendingCard != card) return;
                    pendingCard = null;

                    if (!IsActive || UITaskPanel.ActiveSpawnerCard != card) return;

                    ShowHighlight(card);
                });
            });
        }

        private void CancelPendingHighlight()
        {
            pendingHighlightTween.KillActive();
            pendingHighlightTween = null;
            pendingCard = null;
        }

        private void ShowHighlight(UISpawnerRewardCard card)
        {
            cachedCard = card;
            highlightShown = true;

            RectTransform cardRect = (RectTransform)card.transform;
            Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(new[] { cardRect }, TutorialSpotlightMaskController.RectTransform);
            TutorialSpotlightMaskController.Show(rect);
            TutorialCanvasController.ActivatePointer(card.ClaimButton.transform.position, TutorialPointerAnimations.POINTER_CLICK);
            UITutorialMessageBubble.Show(heroineCharacter, heroineEmotion, hintText, cardRect);

            card.ClaimButton.onClick.AddListener(OnClaimClicked);
        }

        private void HideHighlight()
        {
            highlightShown = false;

            TutorialSpotlightMaskController.Hide();
            TutorialCanvasController.ResetPointer();
            UITutorialMessageBubble.Hide();

            if (cachedCard != null)
                cachedCard.ClaimButton.onClick.RemoveListener(OnClaimClicked);
            cachedCard = null;
        }

        private void OnClaimClicked()
        {
            HideHighlight();
            FinishTutorial();
        }

        private void Cleanup()
        {
            UIController.PageOpened -= OnPageOpened;
            UIController.PageClosed -= OnPageClosed;
            if (TaskController.Instance != null)
                TaskController.Instance.SpawnerQueue.OnChanged -= TryShow;

            CancelPendingHighlight();
            HideHighlight();
        }
    }
}
