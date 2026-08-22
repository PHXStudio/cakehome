using UnityEngine;

namespace Watermelon
{
    public class SystemMessagePreInitializer : MonoBehaviour, IPreInitializable
    {
        public void PreInit()
        {
            ISystemMessage systemMessage = GetComponentInChildren<ISystemMessage>(true);
            if (systemMessage == null)
            {
                LogManager.LogWarning("[Initializer]: No ISystemMessage found in children — system popups will not work.", LogCategory.Systems);
                return;
            }

            systemMessage.Init();
            SystemMessage.Register(systemMessage);
        }
    }
}
