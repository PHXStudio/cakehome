using UnityEngine;
namespace Watermelon
{
    public static class GuestRegistration
    {
        private const string REG_PREFS = "GuestReg";
        private const string REG_REWARD_KEY = "RegRewardClaimed";
        public static bool IsRegistered()
        {
            return PlayerPrefs.GetInt(REG_PREFS, 0) == 1;
        }
        public static bool IsRewardClaimed()
        {
            return PlayerPrefs.GetInt(REG_REWARD_KEY, 0) == 1;
        }
        public static string GetRegistrationMethod()
        {
            return PlayerPrefs.GetString(REG_PREFS + "_method", "none");
        }
        public static void RegisterAsGuest()
        {
            if (IsRegistered()) return;
            PlayerPrefs.SetInt(REG_PREFS, 1);
            PlayerPrefs.SetString(REG_PREFS + "_method", "guest");
            PlayerPrefs.Save();
            CustomAnalytics.TrackRegistration("guest");
            Debug.Log("[GuestReg] Registered as guest");
        }
        public static void ClaimRegistrationReward()
        {
            if (IsRewardClaimed()) return;

            // Grant energy + a grade-1 Kettle spawner for the merge board
            EnergyController.Add(30, ignoreCap: true);
            TaskController.Instance?.SpawnerQueue?.Push("Kettle", 1);

            // Grant 50 coins
            CurrencyController.Add(CurrencyType.Coins, 50);
            PlayerPrefs.SetInt(REG_REWARD_KEY, 1);
            PlayerPrefs.Save();
            CustomAnalytics.TrackCurrencyGain("coins", 50, "registration_reward");
            Debug.Log("[GuestReg] Rewards claimed: 30 energy + Kettle spawner + 50 coins");
        }
        public static bool ShouldShowRegistrationPopup()
        {
            return !IsRegistered() && !IsRewardClaimed();
        }
    }
}
