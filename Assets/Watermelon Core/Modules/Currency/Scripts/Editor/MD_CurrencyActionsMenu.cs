using UnityEngine;
using UnityEditor;

namespace Watermelon
{
    public static class CurrencyActionsMenu
    {
        [MenuItem("Actions/Debug/Currency/Get 200K Coins")]
        private static void GetCoins()
        {
            CurrencyController.Set(CurrencyType.Coins, 200000);
        }

        [MenuItem("Actions/Debug/Currency/Get 200K Coins", true)]
        private static bool GetCoinsValidation()
        {
            return Application.isPlaying;
        }

        [MenuItem("Actions/Debug/Currency/No Money")]
        private static void NoMoney()
        {
            CurrencyController.Set(CurrencyType.Coins, 0);
        }

        [MenuItem("Actions/Debug/Currency/No Money", true)]
        private static bool NoMoneyValidation()
        {
            return Application.isPlaying;
        }

        [MenuItem("Actions/Debug/Currency/Get 500 Gems")]
        private static void GetGems()
        {
            CurrencyController.Set(CurrencyType.Gems, 500);
        }

        [MenuItem("Actions/Debug/Currency/Get 500 Gems", true)]
        private static bool GetGemsValidation()
        {
            return Application.isPlaying;
        }

        [MenuItem("Actions/Debug/Currency/No Gems")]
        private static void NoGems()
        {
            CurrencyController.Set(CurrencyType.Gems, 0);
        }

        [MenuItem("Actions/Debug/Currency/No Gems", true)]
        private static bool NoGemsValidation()
        {
            return Application.isPlaying;
        }
    }
}