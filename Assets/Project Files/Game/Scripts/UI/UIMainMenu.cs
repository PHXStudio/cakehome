using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIMainMenu : UIPage
    {
        [BoxGroup("References", "References")]
        [SerializeField] Button backButton;
        [BoxGroup("References")]
        [SerializeField] Button buildButton;
        [BoxGroup("References")]
        [SerializeField] Button zonesButton;
        [BoxGroup("References")]
        [SerializeField] Button iapStoreButton;
        [BoxGroup("References")]
        [SerializeField] RectTransform zoneContainer;
        [BoxGroup("References")]
        [SerializeField] RectTransform safeAreaRectTransform;


        private ZoneBehavior activeZone;
        private ZoneData activeZoneData;

        public RectTransform BackButtonRectTransform => (RectTransform)backButton.transform;
        public RectTransform BuildButtonRectTransform => (RectTransform)buildButton.transform;

        public override void Init()
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
            buildButton.onClick.AddListener(OnBuildButtonClicked);
            zonesButton.onClick.AddListener(OnZonesButtonClicked);
            iapStoreButton.onClick.AddListener(OnIAPStoreButtonClicked);

            // UIZones can switch the zone while this page is already open underneath it —
            // refresh immediately instead of waiting for the next OnShow (game -> menu transition).
            ZoneController.OnZoneChanged += DisplayZone;

            SafeAreaAdapter.RegisterRectTransform(safeAreaRectTransform);
        }

        protected override void OnUnload()
        {
            ZoneController.OnZoneChanged -= DisplayZone;
        }

        // OnUnload only runs via UIController.ResetPages(), which is never called before this
        // page's scene is unloaded — OnDestroy is the reliable hook that always fires, so the
        // subscription to the persistent ZoneController event doesn't outlive this instance.
        private void OnDestroy()
        {
            ZoneController.OnZoneChanged -= DisplayZone;
        }

        private void DisplayZone(ZoneData zoneData)
        {
            if (activeZoneData == zoneData && activeZone != null) return;

            if (activeZone != null)
                Destroy(activeZone.gameObject);

            activeZoneData = zoneData;

            if (zoneData == null || zoneData.ZonePrefab == null) return;

            activeZone = Instantiate(zoneData.ZonePrefab, zoneContainer);
            activeZone.Init(zoneData);

            UIBuilding.SpawnCards(activeZone);
        }

#region Show/Hide

        protected override void OnShow()
        {
            DisplayZone(BuildingController.CurrentZone);

            NotifyOpened();
        }

        protected override void OnHide()
        {
            NotifyClosed();
        }

#endregion

        private void OnBackButtonClicked()
        {
            MergeViewController.SetBuildingActive(false);

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }

        private void OnBuildButtonClicked()
        {
            UIBuilding.Show();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }

        private void OnZonesButtonClicked()
        {
            UIZones.Show();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }

        private void OnIAPStoreButtonClicked()
        {
            CakeUIBridge.OpenStore?.Invoke();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }
    }
}
