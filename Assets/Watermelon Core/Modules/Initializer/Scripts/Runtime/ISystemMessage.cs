namespace Watermelon
{
    public interface ISystemMessage
    {
        void Init();
        void ShowMessage(string message, float duration = 2.5f);
        void ShowLoadingPanel();
        void ChangeLoadingMessage(string message);
        void HideLoadingPanel();
    }
}
