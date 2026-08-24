using Watermelon.Map;

namespace Watermelon
{
    /// <summary>
    /// 露营车 / 消消乐板块：关卡地图 + 主菜单 HUD。
    /// </summary>
    public sealed class CamperHubModule : IHubModule
    {
        private bool isActive;

        public MainHubTab Tab => MainHubTab.Camper;
        public bool IsActive => isActive;

        public void Enter()
        {
            LevelController.SetBackgroundVisible(true);
            MapBehavior.SetMapVisible(true);
            MapBehavior.EnableScroll();

            if (UIController.GetPage<UIMainMenu>() != null)
                UIController.ShowPage<UIMainMenu>();

            // 与门店 Tab 共用的顶栏（经验徽章 + 能量/金币/钻石）
            MergeViewController.SetHeaderVisible(true);

            isActive = true;
        }

        public void Exit()
        {
            if (UIController.IsDisplayed<UIMainMenu>())
                UIController.HidePage<UIMainMenu>();

            MergeViewController.SetHeaderVisible(false);

            MapBehavior.SetMapVisible(false);
            MapBehavior.DisableScroll();
            isActive = false;
        }
    }
}
