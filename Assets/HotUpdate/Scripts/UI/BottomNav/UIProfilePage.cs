using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// TAB3 我的（个人中心）：Avatar 换装 + 专属称号 + 甜品展馆。
    /// 由 ProfileUIBuilder 构建标准 prefab，运行时刷新数据。
    /// </summary>
    public class UIProfilePage : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;

        [Header("Avatar 换装")]
        [SerializeField] TMP_Text avatarTitleText;
        [SerializeField] Transform avatarSlotRoot;
        [SerializeField] GameObject avatarRowTemplate;

        [Header("称号")]
        [SerializeField] Transform titleRoot;
        [SerializeField] GameObject titleRowTemplate;

        [Header("展馆")]
        [SerializeField] TMP_Text galleryCountText;

        public override void Init()
        {
            AvatarController.Init();
            AvatarController.RefreshTitles();

            if (safeAreaRectTransform != null)
                NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);

            BuildAvatarSlots();
            BuildTitleRows();
            RefreshHud();
        }

        protected override void OnShow()
        {
            AvatarController.RefreshTitles();
            RefreshHud();

            NotifyOpened();
        }

        protected override void OnHide()
        {
            NotifyClosed();
        }

        private void RefreshHud()
        {
            if (galleryCountText != null)
            {
                int owned = AvatarController.GetGalleryCount();
                int total = ShopController.Catalog != null ? ShopController.Catalog.Count : 0;
                galleryCountText.text = $"甜品展馆 {owned}/{total}";
            }

            RefreshSlotVisuals();
            RefreshTitleVisuals();
        }

        // ------------------------------------------------------------------ 换装

        private void BuildAvatarSlots()
        {
            if (avatarSlotRoot == null || avatarRowTemplate == null)
                return;

            int count = AvatarController.SlotItems.Length;
            for (int i = 0; i < count; i++)
            {
                GameObject row = Instantiate(avatarRowTemplate, avatarSlotRoot);
                row.name = "Avatar Slot " + i;
                row.SetActive(true);

                AvatarSlotRow rowComp = row.GetComponent<AvatarSlotRow>();
                if (rowComp == null)
                    rowComp = row.AddComponent<AvatarSlotRow>();

                rowComp.Setup((AvatarController.AvatarSlot)i);
            }
        }

        private void RefreshSlotVisuals()
        {
            if (avatarSlotRoot == null)
                return;

            for (int i = 0; i < avatarSlotRoot.childCount; i++)
            {
                AvatarSlotRow row = avatarSlotRoot.GetChild(i).GetComponent<AvatarSlotRow>();
                if (row != null)
                    row.Refresh();
            }
        }

        // ------------------------------------------------------------------ 称号

        private void BuildTitleRows()
        {
            if (titleRoot == null || titleRowTemplate == null)
                return;

            int count = AvatarController.Titles.Length;
            for (int i = 0; i < count; i++)
            {
                GameObject row = Instantiate(titleRowTemplate, titleRoot);
                row.name = "Title Row " + i;
                row.SetActive(true);

                AvatarTitleRow rowComp = row.GetComponent<AvatarTitleRow>();
                if (rowComp == null)
                    rowComp = row.AddComponent<AvatarTitleRow>();

                rowComp.Setup(i);
            }
        }

        private void RefreshTitleVisuals()
        {
            if (titleRoot == null)
                return;

            for (int i = 0; i < titleRoot.childCount; i++)
            {
                AvatarTitleRow row = titleRoot.GetChild(i).GetComponent<AvatarTitleRow>();
                if (row != null)
                    row.Refresh();
            }
        }
    }
}
