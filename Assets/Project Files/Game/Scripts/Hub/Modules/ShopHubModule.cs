namespace Watermelon
{
    /// <summary>
    /// 门店挂机板块：展柜世界 + 烘焙积分 HUD。
    /// </summary>
    public sealed class ShopHubModule : IHubModule
    {
        private bool isActive;

        public MainHubTab Tab => MainHubTab.Shop;
        public bool IsActive => isActive;

        public void Enter()
        {
            ShopController.EnsureInitialized();
            ShopWorld.SetVisible(true);

            if (UIController.GetPage<UIShopPage>() != null)
                UIController.ShowPage<UIShopPage>();

            isActive = true;
        }

        public void Exit()
        {
            if (UIController.IsDisplayed<UIShopPage>())
                UIController.HidePage<UIShopPage>();

            ShopWorld.Hide();
            isActive = false;
        }
    }
}
