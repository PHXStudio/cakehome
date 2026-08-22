using UnityEngine;

namespace Watermelon
{
    public static class Checkpoint
    {
        [HideInCallstack]
        public static void Log(string label)
        {
#if DEBUG_LOGS_CHECKPOINT
            Debug.Log($"<color=#00FF88>◆ Checkpoint: {label}</color>");
#endif
        }

        [HideInCallstack]
        public static void Log(string label, Object context)
        {
#if DEBUG_LOGS_CHECKPOINT
            Debug.Log($"<color=#00FF88>◆ Checkpoint: {label}</color>", context);
#endif
        }
    }
}
