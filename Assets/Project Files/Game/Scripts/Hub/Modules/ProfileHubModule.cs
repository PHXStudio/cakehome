namespace Watermelon
{
    /// <summary>
    /// 个人中心板块：独立 UI 页。世界显隐由其它板块 Exit/Enter 负责。
    /// </summary>
    public sealed class ProfileHubModule : IHubModule
    {
        private bool isActive;

        public MainHubTab Tab => MainHubTab.Profile;
        public bool IsActive => isActive;

        public void Enter()
        {
            if (UIController.GetPage<UIProfilePage>() != null)
                UIController.ShowPage<UIProfilePage>();

            isActive = true;
        }

        public void Exit()
        {
            if (UIController.IsDisplayed<UIProfilePage>())
                UIController.HidePage<UIProfilePage>();

            isActive = false;
        }
    }
}
