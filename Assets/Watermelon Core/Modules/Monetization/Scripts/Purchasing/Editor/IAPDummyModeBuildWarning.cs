using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Watermelon
{
    public class IAPDummyModeBuildWarning : IPreprocessBuildWithReport
    {
        private const string SUPPRESS_KEY = "Watermelon.BuildWarning.IAPMode.Suppress";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & UnityEditor.BuildOptions.Development) != 0) return;

#if !MODULE_IAP
            const string message = "IAP module is in Dummy mode — purchases won't work in production. Configure the module before releasing to stores.";

            if (Application.isBatchMode)
            {
                Debug.LogWarning("[IAP Manager]: " + message);
                return;
            }

            if (EditorPrefs.GetBool(SUPPRESS_KEY, false)) return;

            int choice = EditorUtility.DisplayDialogComplex(
                "Dummy IAP Mode Active",
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
                throw new BuildFailedException("Build cancelled: Dummy IAP mode is active.");
#endif
        }
    }
}
