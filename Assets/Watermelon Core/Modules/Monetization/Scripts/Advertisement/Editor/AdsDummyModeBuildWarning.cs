using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Watermelon
{
    public class AdsDummyModeBuildWarning : IPreprocessBuildWithReport
    {
        private const string SUPPRESS_KEY = "Watermelon.BuildWarning.AdsMode.Suppress";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & UnityEditor.BuildOptions.Development) != 0) return;

            string[] guids = AssetDatabase.FindAssets("t:AdsSettings");
            if (guids.Length == 0) return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            AdsSettings adsSettings = AssetDatabase.LoadAssetAtPath<AdsSettings>(path);

            if (adsSettings == null || adsSettings.ActiveProvider != "Dummy") return;

            const string message = "Ads module is set to Dummy provider — ads won't show in production. Configure the module before releasing to stores.";

            if (Application.isBatchMode)
            {
                Debug.LogWarning("[AdsManager]: " + message);
                return;
            }

            if (EditorPrefs.GetBool(SUPPRESS_KEY, false)) return;

            int choice = EditorUtility.DisplayDialogComplex(
                "Dummy Ads Mode Active",
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
                throw new BuildFailedException("Build cancelled: Dummy Ads mode is active.");
        }
    }
}
