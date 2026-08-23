using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Static facade for the merge (shop hub) view layer. Lives in Game.Scripts so template pages
    /// (UIGame/UIMainMenu/UITaskPanel) can switch views without referencing the HotUpdate
    /// GameController, and so HotUpdate (MergeHubModule) can show/hide template pages without
    /// naming their types across the assembly boundary.
    /// </summary>
    public static class MergeViewController
    {
        public static bool IsBuildingActive { get; private set; }

        // The merge board page contains nested canvases (Items / Flying Objects) that render
        // independently of the page's own Canvas — hiding the page is not enough to keep the
        // board off other tabs, so the root GameObject is toggled alongside.
        private static GameObject mergeGameRoot;

        private static GameObject MergeGameRoot
        {
            get
            {
                if (mergeGameRoot == null)
                {
                    UIGame page = UIController.GetPage<UIGame>();
                    if (page != null) mergeGameRoot = page.gameObject;
                }
                return mergeGameRoot;
            }
        }

        /// <summary>Switch between the merge board view (false) and the building/zone view (true).</summary>
        public static void SetBuildingActive(bool active)
        {
            if (IsBuildingActive == active) return;
            IsBuildingActive = active;

            if (active)
            {
                UIController.HidePage<UIGame>();
                UIController.ShowPage<UIMainMenu>();
            }
            else
            {
                UIController.HidePage<UIMainMenu>();
                UIController.ShowPage<UIGame>();
            }
        }

        /// <summary>Called by MergeHubModule when the shop tab becomes active.</summary>
        public static void EnterHub()
        {
            if (MergeGameRoot != null) MergeGameRoot.SetActive(true);

            if (IsBuildingActive)
                UIController.ShowPage<UIMainMenu>();
            else
                UIController.ShowPage<UIGame>();

            UIController.ShowPage<UIHeader>();
        }

        /// <summary>Called by MergeHubModule when leaving the shop tab. Hides the hub pages and any overlay pages.</summary>
        public static void ExitHub()
        {
            UIController.HidePage<UIGame>();
            UIController.HidePage<UIMainMenu>();
            UIController.HidePage<UIHeader>();

            // Overlay pages that may be open on top of the hub
            UIController.HidePage<UIBuilding>();
            UIController.HidePage<UIZones>();
            UIController.HidePage<UIInfoWindow>();
            UIController.HidePage<UIRecoverEnergy>();
            UIController.HidePage<UIDialog>();

            if (MergeGameRoot != null) MergeGameRoot.SetActive(false);
        }

        /// <summary>
        /// Activates the merge-side tutorials. Lives here (Game.Scripts) because HotUpdate has its
        /// own TutorialController class — name resolution there would bind the wrong controller.
        /// </summary>
        public static void ActivateTutorials()
        {
            TutorialController.ActivateTutorial<FirstStartTutorial>();
            TutorialController.ActivateTutorial<SpawnerRewardTutorial>();
            TutorialController.ActivateTutorial<BuildingUpgradeHintTutorial>();
        }

        // ─── Onboarding gates (HotUpdate can't touch FirstStartTutorial — its base type lives in
        // the Watermelon.Tutorial assembly, which HotUpdate does not reference) ──────────────

        public static bool IsOnboardingCompleted() => FirstStartTutorial.IsCompleted();

        /// <summary>Keeps interstitials silent while onboarding runs. Pair with <see cref="DetachInterstitialGuard"/>.</summary>
        public static void AttachInterstitialGuard() => AdsManager.InterstitialConditions += FirstStartTutorial.IsCompleted;

        public static void DetachInterstitialGuard() => AdsManager.InterstitialConditions -= FirstStartTutorial.IsCompleted;

        /// <summary>Fires the zone-start (atUpgrade: 0) progression only when onboarding is done.</summary>
        public static void TriggerInitialProgressionIfOnboarded()
        {
            if (FirstStartTutorial.IsCompleted())
                BuildingController.TriggerInitialProgression(ZoneController.CurrentZone);
        }
    }
}
