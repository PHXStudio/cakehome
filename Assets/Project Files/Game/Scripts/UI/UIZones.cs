using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIZones : UIPage
    {
        public override bool IsPopup => true;

        [SerializeField] GameObject fadeObject;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform content;
        [SerializeField] UIZoneCard cardPrefab;
        [SerializeField] float bottomMargin = 100f;
        [SerializeField] float overlayDuration = 0.3f;

        private UIFadeAnimation backFade;
        private readonly List<UIZoneCard> cards = new List<UIZoneCard>();
        private float cardStep;

        public override void Init()
        {
            backFade = new UIFadeAnimation(fadeObject);
            backFade.Hide(immediately: true);

            PopulateCards();

            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnCloseButtonClicked()
        {
            UIController.HidePage<UIZones>();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }

        public static void Show()
        {
            UIController.ShowPage<UIZones>();
        }

        private void PopulateCards()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
            cards.Clear();

            foreach (ZoneData zone in ZoneController.AllZones)
            {
                UIZoneCard card = Instantiate(cardPrefab, content);
                card.Init(zone);
                card.OnClicked += OnCardClicked;
                cards.Add(card);
            }

            // Layout group needs a pass before card rects report their final height.
            Canvas.ForceUpdateCanvases();

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            cardStep = cards.Count > 0
                ? ((RectTransform)cards[0].transform).rect.height + layout.spacing
                : 0f;
        }

        private int GetCurrentZoneIndex()
        {
            ZoneData current = ZoneController.CurrentZone;
            int index = cards.FindIndex(card => card.Zone == current);
            return Mathf.Max(0, index);
        }

        private void OnCardClicked(UIZoneCard card)
        {
            if (!ZoneController.IsUnlocked(card.Zone) || card.Zone == ZoneController.CurrentZone) return;

            string zoneId = card.Zone.ZoneId;

            // Board rebuild happens while the overlay fully covers the screen, so the
            // teardown/rebuild in MergeController.HandleZoneChanged never flashes on screen.
            Overlay.Show(overlayDuration, () =>
            {
                ZoneController.SelectZone(zoneId);
                RefreshCards();
                Overlay.Hide(overlayDuration);
            });
        }

        private void RefreshCards()
        {
            foreach (UIZoneCard card in cards)
                card.Refresh();
        }

        // Cards are stacked bottom-to-top (first zone at the bottom, later zones above),
        // so the current zone's card sits at cardStep * index from the content's bottom edge.
        // Scrolls so that point ends up close to the bottom of the viewport, reading as
        // "this is where you are" instead of centered/neutral.
        private void ScrollToCurrentZone()
        {
            if (cards.Count == 0 || cardStep <= 0f) return;

            float viewportHeight = ((RectTransform)scrollRect.viewport).rect.height;
            float scrollableHeight = content.rect.height - viewportHeight;

            if (scrollableHeight <= 0f)
            {
                scrollRect.verticalNormalizedPosition = 0f;
                return;
            }

            int index = GetCurrentZoneIndex();
            float cardBottomFromContentBottom = index * cardStep;
            float normalized = (cardBottomFromContentBottom - bottomMargin) / scrollableHeight;

            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized);
        }

        protected override void OnShow()
        {
            backFade.Show(0.2f);

            RefreshCards();

            Canvas.ForceUpdateCanvases();
            ScrollToCurrentZone();

            NotifyOpened();
        }

        protected override void OnHide()
        {
            backFade.Hide(immediately: true, onCompleted: () =>
            {
                NotifyClosed();
            });
        }

        protected override void OnUnload()
        {
            foreach (UIZoneCard card in cards)
                card.OnClicked -= OnCardClicked;
        }
    }
}
