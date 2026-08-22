using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Watermelon
{
    public class LoggingDefinesBuildWarning : IPreprocessBuildWithReport
    {
        private const string SUPPRESS_KEY = "Watermelon.BuildWarning.LoggingDefines.Suppress";

        public int callbackOrder => 0;

        private static readonly (string define, string label)[] LogDefines =
        {
            ("DEBUG_LOGS_GAME",       "Game"),
            ("DEBUG_LOGS_SYSTEMS",    "Systems"),
            ("DEBUG_LOGS_SERVICES",   "Services"),
            ("DEBUG_LOGS_CHECKPOINT", "Checkpoint"),
        };

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & UnityEditor.BuildOptions.Development) != 0) return;

            var active = new List<string>();

            foreach (var (define, label) in LogDefines)
            {
                if (DefineManager.HasDefine(define))
                    active.Add(label);
            }

            if (active.Count == 0) return;

            string categories = string.Join(", ", active);
            string message = $"Debug log categories are enabled: {categories}. " +
                             "Extra logging is not recommended in a release build — it affects performance and exposes internal flow to the log output. " +
                             "Disable via Actions → Log Categories → All Off.";

            if (Application.isBatchMode)
            {
                Debug.LogWarning("[Logging]: " + message);
                return;
            }

            if (EditorPrefs.GetBool(SUPPRESS_KEY, false)) return;

            int choice = EditorUtility.DisplayDialogComplex(
                "Debug Logging Active",
                message + "\n\nContinue build?",
                "Continue",
                "Don't show again",
                "Cancel"
            );

            if (choice == 1)
            {
                EditorPrefs.SetBool(SUPPRESS_KEY, true);
                return;
            }

            if (choice == 2)
                throw new BuildFailedException("Build cancelled: debug log categories are still enabled.");
        }
    }
}
