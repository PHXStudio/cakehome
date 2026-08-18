using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIProfilePage : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] Text titleText;
        [SerializeField] Text subtitleText;

        public override void Init()
        {
            BottomNavTextUtil.Apply(titleText, "我的", 72);
            BottomNavTextUtil.Apply(subtitleText, "个人中心即将推出", 36);

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
