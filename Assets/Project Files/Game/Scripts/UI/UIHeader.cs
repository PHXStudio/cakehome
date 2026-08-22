using UnityEngine;

namespace Watermelon
{
    public class UIHeader : UIPage
    {
        [BoxGroup("References", "References")]
        [SerializeField] ExperienceBadge experienceBadge;

        [BoxGroup("Top Panel", "Top Panel")]
        [SerializeField] CurrencyUIPanelSimple coinsPanel;
        [BoxGroup("Top Panel", "Top Panel")]
        [SerializeField] CurrencyUIPanelSimple gemsPanel;
        [BoxGroup("Top Panel", "Top Panel")]
        [SerializeField] EnergyUIPanel energyPanel;

        [BoxGroup("Reward Fly", "Reward Fly")]
        [SerializeField] GameObject flyingRewardPrefab;

        public ExperienceBadge ExperienceBadge => experienceBadge;
        public CurrencyUIPanelSimple CoinsPanel => coinsPanel;
        public CurrencyUIPanelSimple GemsPanel => gemsPanel;
        public EnergyUIPanel EnergyPanel => energyPanel;

        // Shared origin for reward-fly animations (exp orbs, spawner reward icon) that should
        // visually originate from the middle of the screen regardless of which page is active.
        private RectTransform screenCenterRectTransform;
        public RectTransform ScreenCenterRectTransform => screenCenterRectTransform;

        // Custom CurrencyCloud prefab for reward icons (shine + trail) — see RewardFlyElement.
        public GameObject FlyingRewardPrefab => flyingRewardPrefab;

        public override void Init()
        {
            CreateScreenCenterAnchor();

            experienceBadge.Init();

            coinsPanel.Init();
            gemsPanel.Init();
            energyPanel.Init();
        }

        private void CreateScreenCenterAnchor()
        {
            GameObject anchorObject = new GameObject("Screen Center Anchor", typeof(RectTransform));
            RectTransform rectTransform = (RectTransform)anchorObject.transform;
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            screenCenterRectTransform = rectTransform;
        }

#region Show/Hide

        protected override void OnHide()
        {
            experienceBadge.Disable();

            coinsPanel.Disable();
            gemsPanel.Disable();
            energyPanel.Disable();

            NotifyClosed();
        }

        protected override void OnShow()
        {
            experienceBadge.Activate();

            coinsPanel.Activate();
            gemsPanel.Activate();
            energyPanel.Activate();

            NotifyOpened();
        }

#endregion

    }
}
