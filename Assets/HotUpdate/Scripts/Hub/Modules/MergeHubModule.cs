namespace Watermelon
{
    /// <summary>
    /// 门店合成板块：mergedev 合成玩法（棋盘 + 顾客订单 + 店铺装修）。
    /// 页面显隐经 Game.Scripts 侧的 MergeViewController 门面操作
    /// （HotUpdate 无法直接引用 Game.Scripts 的模板页面类型）。
    /// </summary>
    public sealed class MergeHubModule : IHubModule
    {
        private bool isActive;

        public MainHubTab Tab => MainHubTab.Shop;
        public bool IsActive => isActive;

        public void Enter()
        {
            // Level menu background prefab covers the merge board UI
            LevelController.SetBackgroundVisible(false);

            MergeViewController.OnBuildingActiveChanged += HandleBuildingActiveChanged;
            MergeViewController.EnterHub();
            // 应用当前视图状态：落在棋盘页时立即隐藏底栏
            HandleBuildingActiveChanged(MergeViewController.IsBuildingActive);

            isActive = true;
        }

        public void Exit()
        {
            MergeViewController.OnBuildingActiveChanged -= HandleBuildingActiveChanged;
            MergeViewController.ExitHub();

            // 离开门店板块必须恢复底栏（露营车 Tab 依赖它）
            UIBottomNavBar.Show();

            isActive = false;
        }

        // 棋盘页隐藏底栏 Tab（页面更大、避免误触），门店/装修视图恢复。
        // 棋盘页回程路径：地图按钮 → 门店视图（底栏回来）→ 切 Camper。
        private static void HandleBuildingActiveChanged(bool buildingActive)
        {
            if (buildingActive)
                UIBottomNavBar.Show();
            else
                UIBottomNavBar.Hide();
        }
    }
}
