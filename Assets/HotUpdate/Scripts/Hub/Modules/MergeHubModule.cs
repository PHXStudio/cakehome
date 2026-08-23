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

            MergeViewController.EnterHub();

            isActive = true;
        }

        public void Exit()
        {
            MergeViewController.ExitHub();

            isActive = false;
        }
    }
}
