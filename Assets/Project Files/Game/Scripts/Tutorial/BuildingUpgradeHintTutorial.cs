using System;
using UnityEngine;

namespace Watermelon
{
    // One-shot: highlights the pinned hammer/upgrade-hint task card the first time the player can
    // afford a building upgrade while on the board (UIGame), teaching them to tap it to go build.
    // Suppressed until FirstStartTutorial finishes, so it never fights that tutorial's own
    // Step.TeachBuildingUpgrade highlight over the same singleton spotlight/pointer/bubble.
    // Parented under TutorialController in Game.unity (auto-discovered).
    public class BuildingUpgradeHintTutorial : BaseTutorial
    {
        [Header("Heroine")]
        [SerializeField] CharacterData heroineCharacter;
        [SerializeField] EmotionType heroineEmotion = EmotionType.Default;

        [Header("Hint")]
        [SerializeField, TextArea] string hintText = "You can afford an upgrade! Tap here to go build.";

        private UIHammerCard cachedCard;
        private bool highlightShown;
        private UIHammerCard pendingCard;
        private TweenCase pendingHighlightTween;

        protected override void OnInitialised() { }

        protected override void OnStarted()
        {
            UIController.PageOpened += OnPageOpened;
            UIController.PageClosed += OnPageClosed;
            CurrencyController.SubscribeGlobalCallback(OnCurrencyChanged);
            BuildingController.OnUpgradeCompleted += OnBuildingUpgraded;

            TryShow();
        }

        protected override void OnFinished() => Cleanup();

        protected override void OnUnloaded() => Cleanup();

        private void OnCurrencyChanged(Currency currency, int difference) => TryShow();
        private void OnBuildingUpgraded(ZoneData zone, string buildingId, int totalUpgradesInZone) => TryShow();
        private void OnPageClosed(UIPage page, Type pageType) => TryShow();

        // Also doubles as the "player arrived" detector: the hammer card's own click handler
        // (UITaskPanel.ShowHammerCard) already navigates straight to UIBuilding, so there's no
        // need for a separate click hook on the card itself (it exposes no public Button, unlike
        // UISpawnerRewardCard).
        private void OnPageOpened(UIPage page, Type pageType)
        {
            // UIBuilding is a popup (stays layered over UIGame), so TryShow's onBoard check
            // never sees us leave the board — this is the only place that can catch arriving
            // here. Must also cancel a still-pending highlight (card tapped before its enter
            // animation settled): otherwise ShowHighlight fires later on top of the building
            // window, with no further PageOpened(UIBuilding) left to dismiss it.
            if (pageType == typeof(UIBuilding))
            {
                CancelPendingHighlight();
                if (highlightShown) HideHighlight();
                FinishTutorial();
                return;
            }

            TryShow();
        }

        // Deferred one frame — mirrors SpawnerRewardTutorial.TryShow: the events this reacts to
        // are the same ones UITaskPanel.RefreshHammerCard reacts to, so the card may not exist yet
        // on the same frame.
        private void TryShow()
        {
            Tween.NextFrame(() =>
            {
                if (!IsActive) return;

                // Don't compete with FirstStartTutorial's own onboarding highlights.
                if (!FirstStartTutorial.IsCompleted())
                {
                    CancelPendingHighlight();
                    if (highlightShown) HideHighlight();
                    return;
                }

                // TODO(模板迁移): 原逻辑取模板 UIGame 页面(HotUpdate 程序集),暂按未显示处理
                bool onBoard = false;

                if (!onBoard)
                {
                    CancelPendingHighlight();
                    if (highlightShown) HideHighlight();
                    return;
                }

                UIHammerCard card = UITaskPanel.ActiveHammerCard;
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

                // The card is still growing in (UIHammerCard.PlayEnter tweens its LayoutElement's
                // preferredWidth over TaskCardAnimation.EnterDuration) — capturing the spotlight
                // rect right now would encapsulate only its current, still-shrunk width. Wait for
                // the enter animation to settle first.
                pendingCard = card;
                pendingHighlightTween = Tween.DelayedCall(TaskCardAnimation.EnterDuration, () =>
                {
                    pendingHighlightTween = null;
                    if (pendingCard != card) return;
                    pendingCard = null;

                    if (!IsActive || UITaskPanel.ActiveHammerCard != card) return;

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

        private void ShowHighlight(UIHammerCard card)
        {
            cachedCard = card;
            highlightShown = true;

            RectTransform cardRect = (RectTransform)card.transform;
            Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(new[] { cardRect }, TutorialSpotlightMaskController.RectTransform);
            TutorialSpotlightMaskController.Show(rect);
            TutorialCanvasController.ActivatePointer(cardRect.position, TutorialPointerAnimations.POINTER_CLICK);
            UITutorialMessageBubble.Show(heroineCharacter, heroineEmotion, hintText, cardRect);
        }

        private void HideHighlight()
        {
            highlightShown = false;

            TutorialSpotlightMaskController.Hide();
            TutorialCanvasController.ResetPointer();
            UITutorialMessageBubble.Hide();

            cachedCard = null;
        }

        private void Cleanup()
        {
            UIController.PageOpened -= OnPageOpened;
            UIController.PageClosed -= OnPageClosed;
            CurrencyController.UnsubscribeGlobalCallback(OnCurrencyChanged);
            BuildingController.OnUpgradeCompleted -= OnBuildingUpgraded;

            CancelPendingHighlight();
            HideHighlight();
        }
    }
}
