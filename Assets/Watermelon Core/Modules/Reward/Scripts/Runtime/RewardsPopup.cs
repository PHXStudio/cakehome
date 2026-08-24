using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Static facade for displaying the rewards popup.
    ///
    /// The concrete implementation (e.g. UIRewardsPopup) registers itself via Register()
    /// on initialization, decoupling RewardsHolder from any specific UIPage subclass.
    /// </summary>
    public static class RewardsPopup
    {
        private static IRewardsPopup instance;

        /// <summary>Called by the concrete popup behavior on initialization.</summary>
        public static void Register(IRewardsPopup popup) => instance = popup;

        /// <summary>
        /// Displays the rewards popup. Returns true if the popup was shown.
        /// Returns false if no IRewardsPopup is registered or previews list is empty.
        /// </summary>
        public static bool Display(List<IRewardPreview> previews, SimpleCallback closeCallback = null)
        {
            if (previews.IsNullOrEmpty())
                return false;

            if (instance == null)
            {
                LogManager.LogWarning("[RewardsPopup] No IRewardsPopup registered. Skipping popup display.", LogCategory.Systems);
                return false;
            }

            return instance.Display(previews, closeCallback);
        }
    }
}
