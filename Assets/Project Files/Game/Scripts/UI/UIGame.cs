using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIGame : UIPage
    {
        [BoxGroup("References", "References")]
        [SerializeField] Image backgroundImage;

        [BoxGroup("Bottom Panel", "Bottom Panel")]
        [SerializeField] Button mapButton;

        private AspectRatioFitter backgroundAspectRatioFitter;

        public RectTransform MapButtonRectTransform => (RectTransform)mapButton.transform;

        public override void Init()
        {
            backgroundAspectRatioFitter = backgroundImage.GetComponent<AspectRatioFitter>();

            RectTransform backgroundParentRectTransform = (RectTransform)backgroundImage.transform.parent;
            Vector2 offsetMax = backgroundParentRectTransform.offsetMax;
            offsetMax.y += SafeAreaAdapter.GetTopOffset();
            backgroundParentRectTransform.offsetMax = offsetMax;

            mapButton.onClick.AddListener(OnMapButtonClicked);

            ZoneController.OnZoneChanged += UpdateBackground;
            UpdateBackground(ZoneController.CurrentZone);
        }

        protected override void OnUnload()
        {
            ZoneController.OnZoneChanged -= UpdateBackground;
        }

        // OnUnload only runs via UIController.ResetPages(), which is never called before this
        // page's scene is unloaded — OnDestroy is the reliable hook that always fires, so the
        // subscription to the persistent ZoneController event doesn't outlive this instance.
        private void OnDestroy()
        {
            ZoneController.OnZoneChanged -= UpdateBackground;
        }

        private void UpdateBackground(ZoneData zone)
        {
            Sprite sprite = zone?.BackgroundSprite;
            if (sprite == null) return;

            backgroundImage.sprite = sprite;
            backgroundAspectRatioFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        private void OnMapButtonClicked()
        {
            MergeViewController.SetBuildingActive(true);

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }

#region Show/Hide

        protected override void OnHide()
        {
            NotifyClosed();
        }

        protected override void OnShow()
        {
            NotifyOpened();
        }

#endregion

    }
}
