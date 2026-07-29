using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIShopPage : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;

        public override void Init()
        {
            BottomNavTextUtil.Apply(titleText, "门店", 72);
            BottomNavTextUtil.Apply(subtitleText, "门店内容即将推出", 36);

            if (safeAreaRectTransform != null)
                NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        public override void PlayShowAnimation()
        {
            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            UIController.OnPageClosed(this);
        }
    }
}
