using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIBuilding : UIPage
    {
        public override bool IsPopup => true;

        [BoxGroup("References", "References")]
        [SerializeField] GameObject fadeObject;
        [BoxGroup("References")]
        [SerializeField] Button closeButton;
        [BoxGroup("References")]
        [SerializeField] ScrollRect scrollRect;
        [BoxGroup("References")]
        [SerializeField] Transform cardsContainer;
        [BoxGroup("References")]
        [SerializeField] BuildingUIBehavior cardPrefab;
        [BoxGroup("References")]
        [SerializeField] GameObject emptyStateObject;

        private UIFadeAnimation backFade;

        public override void Init()
        {
            backFade = new UIFadeAnimation(fadeObject);

            closeButton.onClick.AddListener(OnCloseButtonClicked);

            backFade.Hide(immediately: true);
        }

        public static void SpawnCards(ZoneBehavior zone)
        {
            UIBuilding page = UIController.GetPage<UIBuilding>();
            if (page == null) return;

            page.PopulateCards(zone);
        }

        public static void Show()
        {
            UIBuilding page = UIController.GetPage<UIBuilding>();
            if (page == null) return;

            page.RefreshCards();
            UIController.ShowPage(page);
        }

        public static BuildingUIBehavior FindCard(string buildingId)
        {
            UIBuilding page = UIController.GetPage<UIBuilding>();
            if (page == null) return null;

            for (int i = 0; i < page.cardsContainer.childCount; i++)
            {
                BuildingUIBehavior card = page.cardsContainer.GetChild(i).GetComponent<BuildingUIBehavior>();
                if (card != null && card.BuildingId == buildingId)
                    return card;
            }

            return null;
        }

        private void PopulateCards(ZoneBehavior zone)
        {
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
                Destroy(cardsContainer.GetChild(i).gameObject);

            int remaining = 0;
            if (zone != null && zone.Buildings != null)
            {
                foreach (BuildingBehavior building in zone.Buildings)
                {
                    if (building == null) continue;
                    if (BuildingController.GetBuildingStep(building) >= building.MaxUpgrades) continue;

                    Instantiate(cardPrefab, cardsContainer).Init(building, remaining);
                    remaining++;
                }
            }

            emptyStateObject.SetActive(remaining == 0);
        }

        private void RefreshCards()
        {
            int remaining = 0;
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                BuildingUIBehavior card = cardsContainer.GetChild(i).GetComponent<BuildingUIBehavior>();
                if (card.IsBuildingMaxed)
                {
                    Destroy(card.gameObject);
                }
                else
                {
                    card.Refresh();
                    remaining++;
                }
            }

            emptyStateObject.SetActive(remaining == 0);
        }

        private void ResetScroll()
        {
            scrollRect.velocity = Vector2.zero;
            scrollRect.verticalNormalizedPosition = 1f;
        }

#region Show/Hide

        protected override void OnShow()
        {
            backFade.Show(0.2f);

            ResetScroll();
            NotifyOpened();
        }

        protected override void OnHide()
        {
            backFade.Hide(immediately: true, onCompleted: () =>
            {
                NotifyClosed();
            });
        }

#endregion

        private void OnCloseButtonClicked()
        {
            UIController.HidePage<UIBuilding>();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }
    }
}
