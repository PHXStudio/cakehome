using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [Serializable]
    [RegisterReward(typeof(CurrencyRewardView))]
    public sealed class CurrencyReward : Reward
    {
        [SerializeField] CurrencyAmount[] currencies;
        public CurrencyAmount[] Currencies => currencies;

        [Space]
        [SerializeField] string analyticsSource;

        public CurrencyReward() { }
        public CurrencyReward(CurrencyAmount[] currencies, string analyticsSource = "")
        {
            this.currencies = currencies;
            this.analyticsSource = analyticsSource;
        }

        public override void ApplyReward()
        {
#if MODULE_ANALYTICS
            Dictionary<CurrencyType, int> analyticsDictionary = new Dictionary<CurrencyType, int>();
#endif

            foreach (CurrencyAmount currency in currencies)
            {
                CurrencyController.Add(currency.CurrencyType, currency.Amount);

#if MODULE_ANALYTICS
                analyticsDictionary.TryAdd(currency.CurrencyType, currency.Amount);
#endif
            }

#if MODULE_ANALYTICS
            if (!string.IsNullOrEmpty(analyticsSource))
            {
                Analytics.TrackEvent(AnalyticsEvents.CurrencySource, new AnalyticsCurrencyData(analyticsSource, analyticsDictionary));
            }
#endif
        }

        public int GetAmount(CurrencyType currencyType)
        {
            foreach (CurrencyAmount currency in currencies)
            {
                if (currency.CurrencyType == currencyType)
                    return currency.Amount;
            }

            return 0;
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            if(!currencies.IsNullOrEmpty())
            {
                List<IRewardPreview> previews = new List<IRewardPreview>();
                foreach (CurrencyAmount currencyAmount in currencies)
                {
                    Currency currency = currencyAmount.Currency;

                    CurrencyRewardPreviewSettings previewSettings = currency.PreviewSettings;
                    if (previewSettings != null)
                    {
                        previews.Add(previewSettings.GetPreview(currencyAmount));
                    }
                    else
                    {
                        previews.Add(new RewardPreview(currency.Icon, $"{CurrencyHelper.Format(currencyAmount.Amount)}", CurrencyRewardPreviewSettings.DEFAULT_SORING_ORDER));
                    }
                }

                return previews;
            }

            return null;
        }
    }
}
