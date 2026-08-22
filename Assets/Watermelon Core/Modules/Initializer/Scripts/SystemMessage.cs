using UnityEngine;

namespace Watermelon
{
    public static class SystemMessage
    {
        private static ISystemMessage instance;

        public static void Register(ISystemMessage impl)
        {
            instance = impl;
        }

        public static void ShowMessage(string message, float duration = 2.5f)
        {
            if (instance == null)
            {
                LogManager.LogWarning("[System Message]: ShowMessage called but module is not initialized.", LogCategory.Systems);
                return;
            }

            instance.ShowMessage(message, duration);
        }

        public static void ShowLoadingPanel()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[System Message]: ShowLoadingPanel called but module is not initialized.", LogCategory.Systems);
                return;
            }

            instance.ShowLoadingPanel();
        }

        public static void ChangeLoadingMessage(string message)
        {
            if (instance == null)
            {
                LogManager.LogWarning("[System Message]: ChangeLoadingMessage called but module is not initialized.", LogCategory.Systems);
                return;
            }

            instance.ChangeLoadingMessage(message);
        }

        public static void HideLoadingPanel()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[System Message]: HideLoadingPanel called but module is not initialized.", LogCategory.Systems);
                return;
            }

            instance.HideLoadingPanel();
        }
    }
}
