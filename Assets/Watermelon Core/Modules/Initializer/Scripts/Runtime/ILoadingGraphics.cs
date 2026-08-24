namespace Watermelon
{
    public interface ILoadingGraphics
    {
        void Init(GameLoading gameLoading);
        void SetLoadingState(float progress, string message);
        void ShowErrorMessage(string message);
        void HideErrorMessage();
        void OnLoadingFinished();

    }
}