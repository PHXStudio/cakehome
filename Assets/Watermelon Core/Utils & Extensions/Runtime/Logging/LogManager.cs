using UnityEngine;

namespace Watermelon
{
    public static class LogManager
    {
        [HideInCallstack]
        public static void Log(string message, LogCategory category = LogCategory.Game)
        {
#if DEBUG_LOGS_GAME
            if (category == LogCategory.Game) { Debug.Log(message); return; }
#endif
#if DEBUG_LOGS_SYSTEMS
            if (category == LogCategory.Systems) { Debug.Log(message); return; }
#endif
#if DEBUG_LOGS_SERVICES
            if (category == LogCategory.Services) { Debug.Log(message); return; }
#endif
#if DEBUG_LOGS_CHECKPOINT
            if (category == LogCategory.Checkpoint) { Debug.Log(message); return; }
#endif
        }

        [HideInCallstack]
        public static void LogWarning(string message, LogCategory category = LogCategory.Game)
        {
#if DEBUG_LOGS_GAME
            if (category == LogCategory.Game) { Debug.LogWarning(message); return; }
#endif
#if DEBUG_LOGS_SYSTEMS
            if (category == LogCategory.Systems) { Debug.LogWarning(message); return; }
#endif
#if DEBUG_LOGS_SERVICES
            if (category == LogCategory.Services) { Debug.LogWarning(message); return; }
#endif
#if DEBUG_LOGS_CHECKPOINT
            if (category == LogCategory.Checkpoint) { Debug.LogWarning(message); return; }
#endif
        }

        [HideInCallstack]
        public static void LogError(string message, LogCategory category = LogCategory.Game)
        {
#if DEBUG_LOGS_GAME
            if (category == LogCategory.Game) { Debug.LogError(message); return; }
#endif
#if DEBUG_LOGS_SYSTEMS
            if (category == LogCategory.Systems) { Debug.LogError(message); return; }
#endif
#if DEBUG_LOGS_SERVICES
            if (category == LogCategory.Services) { Debug.LogError(message); return; }
#endif
#if DEBUG_LOGS_CHECKPOINT
            if (category == LogCategory.Checkpoint) { Debug.LogError(message); return; }
#endif
        }
    }
}
