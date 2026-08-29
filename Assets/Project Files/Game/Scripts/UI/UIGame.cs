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

        // Hub 底部导航条（HotUpdate 侧 BottomNavLayout.Height = 120）常驻屏幕底部，
        // 页面 Safe Area 需要上抬对应高度，否则选中面板/地图按钮会被导航条盖住。
        // Game.Scripts 无法引用 HotUpdate，常量在此保留一份（须与 BottomNavLayout.Height 保持一致）。
        private const float BOTTOM_NAV_HEIGHT = 120f;

        private RectTransform safeAreaRectTransform;
        private AspectRatioFitter backgroundAspectRatioFitter;

        public RectTransform MapButtonRectTransform => (RectTransform)mapButton.transform;

        public override void Init()
        {
            backgroundAspectRatioFitter = backgroundImage.GetComponent<AspectRatioFitter>();

            RectTransform backgroundParentRectTransform = (RectTransform)backgroundImage.transform.parent;
            Vector2 offsetMax = backgroundParentRectTransform.offsetMax;
            offsetMax.y += SafeAreaAdapter.GetTopOffset();
            backgroundParentRectTransform.offsetMax = offsetMax;

            SafeAreaElement safeAreaElement = GetComponentInChildren<SafeAreaElement>(true);
            if (safeAreaElement != null)
                safeAreaRectTransform = (RectTransform)safeAreaElement.transform;

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
            ApplyBottomNavPadding();
            NotifyOpened();
        }

        // 与 UIMainMenu.ApplyBottomNavPadding 同款：把 Safe Area 底部抬到导航条之上。
        // SafeAreaAdapter 刷新时只重写 anchors、保留 offsets，因此这里的 padding 不会被冲掉。
        private void ApplyBottomNavPadding()
        {
            if (safeAreaRectTransform == null)
                return;

            Vector2 offsetMin = safeAreaRectTransform.offsetMin;
            offsetMin.y = Mathf.Max(offsetMin.y, BOTTOM_NAV_HEIGHT);
            safeAreaRectTransform.offsetMin = offsetMin;
        }

#endregion

    }
}
