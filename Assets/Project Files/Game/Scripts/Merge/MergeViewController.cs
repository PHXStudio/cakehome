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
        }
    }
}
