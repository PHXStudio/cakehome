namespace Watermelon
{
    public static class UIControllerHelper
    {
        public static TweenCase WaitForPopupsClose(SimpleCallback callback)
        {
            if (!UIController.IsPopupOpened)
            {
                callback?.Invoke();
                return null;
            }

            return Tween.DoWaitForCondition(_ =>
            {
                if (!UIController.IsPopupOpened)
                    callback?.Invoke();
            }, unscaledTime: true);
        }
    }
}
