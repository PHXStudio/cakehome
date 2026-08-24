using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class BuildingUIBehavior : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Image fillbar;
        [SerializeField] Image starPrefab;
        [SerializeField] Transform starsContainer;
        [SerializeField] Sprite activeStarSprite;
        [SerializeField] Sprite inactiveStarSprite;
        [SerializeField] TMP_Text costText;
        [SerializeField] Button buildButton;

        private BuildingBehavior building;
        private readonly List<Image> stars = new List<Image>();

        public bool IsBuildingMaxed => building == null || BuildingController.GetBuildingStep(building) >= building.MaxUpgrades; // null building = design-time placeholder card, treat as maxed so it gets pruned
        public string BuildingId => building != null ? building.BuildingId : null; // null until Init (design-time placeholder cards)
        public RectTransform BuildButtonRectTransform => (RectTransform)buildButton.transform;

        public void Init(BuildingBehavior building, int index)
        {
            this.building = building;

            SpawnStars(building.MaxUpgrades);
            Refresh();

            buildButton.onClick.RemoveAllListeners();
            buildButton.onClick.AddListener(OnBuildClicked);
        }

        public void Refresh()
        {
            int step = BuildingController.GetBuildingStep(building);
            int max = building.MaxUpgrades;

            icon.sprite = step <= 0 ? building.DefaultSprite : building.Upgrades[step - 1].UpgradedSprite;
            fillbar.fillAmount = (float)step / max;

            UpdateStars(step);

            int cost = building.Upgrades[step].Cost;
            costText.text = cost.ToString();
            buildButton.interactable = CurrencyController.HasAmount(CurrencyType.Coins, cost);
        }

        private void SpawnStars(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Image star = Instantiate(starPrefab, starsContainer);

                float t = (i + 1f) / count;
                RectTransform starRect = star.rectTransform;
                starRect.anchorMin = new Vector2(t, starRect.anchorMin.y);
                starRect.anchorMax = new Vector2(t, starRect.anchorMax.y);
                starRect.anchoredPosition = new Vector2(0f, starRect.anchoredPosition.y);

                stars.Add(star);
            }
        }

        private void UpdateStars(int activeCount)
        {
            for (int i = 0; i < stars.Count; i++)
            {
                stars[i].sprite = i < activeCount ? activeStarSprite : inactiveStarSprite;
            }
        }

        private void OnBuildClicked()
        {
            if (!BuildingController.CanUpgrade(building)) return;

            UIController.HidePage<UIBuilding>();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            BuildingController.Upgrade(building);
        }
    }
}
