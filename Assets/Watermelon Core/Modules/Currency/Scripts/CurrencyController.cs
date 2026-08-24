using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class CurrencyController
    {
        private static CurrencyController instance;

        private Currency[] currencies;
        public static Currency[] Currencies
        {
            get
            {
                if (!IsInitialized) { LogNotInitialized(); return null; }
                return instance.currencies;
            }
        }

        private Dictionary<CurrencyType, int> currenciesLink;

        public static bool IsInitialized => instance != null;

        public CurrencyController(CurrencyDatabase currenciesDatabase)
        {
            if (instance != null)
            {
                Debug.LogWarning("[CurrencyController]: Already initialized.");
                return;
            }

            instance = this;

            currencies = currenciesDatabase.Currencies;

            foreach (Currency currency in currencies)
            {
                currency.Init();
            }

#if MODULE_REMOTE_CONFIG
            CurrencyRemoteConfigData remoteConfigData = RemoteConfigController.TryGetConfig<CurrencyRemoteConfigData>("currencies");
#endif

            currenciesLink = new Dictionary<CurrencyType, int>();
            for (int i = 0; i < currencies.Length; i++)
            {
                if (!currenciesLink.ContainsKey(currencies[i].CurrencyType))
                {
                    currenciesLink.Add(currencies[i].CurrencyType, i);
                }
                else
                {
                    Debug.LogError(string.Format("[CurrencyController]: Currency with type {0} added to database twice!", currencies[i].CurrencyType));
                }

                Currency.Save save = SaveController.GetSaveObject<Currency.Save>("currency" + ":" + (int)currencies[i].CurrencyType);
                if (save.Amount == -1)
                {
                    int defaultAmount = currencies[i].DefaultAmount;

#if MODULE_REMOTE_CONFIG
                    if (remoteConfigData != null)
                    {
                        CurrencyRemoteConfigData.Currency currencyOverride = remoteConfigData.GetCurrencyOverride(currencies[i].CurrencyType);
                        if (currencyOverride != null)
                        {
                            defaultAmount = currencyOverride.defaultCount;
                        }
                    }
#endif

                    save.Amount = defaultAmount;
                }

                currencies[i].SetSave(save);
            }
        }

        public void Unload()
        {
            foreach (Currency currency in currencies)
            {
                currency.ClearListeners();
            }

            instance = null;
        }

        public static bool HasAmount(CurrencyType currencyType, int amount)
        {
            if (!IsInitialized) { LogNotInitialized(); return false; }
            return instance.currencies[instance.currenciesLink[currencyType]].Amount >= amount;
        }

        public static int Get(CurrencyType currencyType)
        {
            if (!IsInitialized) { LogNotInitialized(); return 0; }
            return instance.currencies[instance.currenciesLink[currencyType]].Amount;
        }

        public static Currency GetCurrency(CurrencyType currencyType)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ProjectInitSettings projectInitSettings = RuntimeEditorUtils.GetAsset<ProjectInitSettings>();
                if (projectInitSettings != null)
                {
                    CurrencyInitModule currencyInitModule = projectInitSettings.GetModule<CurrencyInitModule>();
                    if (currencyInitModule != null)
                    {
                        CurrencyDatabase currencyDatabase = currencyInitModule.Database;
                        if (currencyDatabase != null)
                        {
                            return currencyDatabase.Currencies.Find(x => x.CurrencyType.Equals(currencyType));
                        }
                    }
                }

                return null;
            }
#endif

            if (!IsInitialized) { LogNotInitialized(); return null; }
            return instance.currencies[instance.currenciesLink[currencyType]];
        }

        public static void Set(CurrencyType currencyType, int amount)
        {
            if (!IsInitialized) { LogNotInitialized(); return; }

            Currency currency = instance.currencies[instance.currenciesLink[currencyType]];
            currency.Amount = amount;

            SaveController.MarkAsSaveIsRequired();
            currency.InvokeChangeEvent(0);
        }

        public static void Add(CurrencyType currencyType, int amount, string analyticsEvent = "")
        {
            if (!IsInitialized) { LogNotInitialized(); return; }

            Currency currency = instance.currencies[instance.currenciesLink[currencyType]];
            currency.Amount += amount;

            SaveController.MarkAsSaveIsRequired();
            currency.InvokeChangeEvent(amount);

            if (!string.IsNullOrEmpty(analyticsEvent))
            {
#if MODULE_ANALYTICS
                Analytics.TrackEvent(AnalyticsEvents.CurrencySource, new AnalyticsCurrencyData(analyticsEvent, new Dictionary<CurrencyType, int>() { { currencyType, amount } }));
#endif
            }
        }

        public static void Substract(CurrencyType currencyType, int amount, string analyticsEvent = "")
        {
            if (!IsInitialized) { LogNotInitialized(); return; }

            Currency currency = instance.currencies[instance.currenciesLink[currencyType]];
            currency.Amount -= amount;

            SaveController.MarkAsSaveIsRequired();
            currency.InvokeChangeEvent(-amount);

            if (!string.IsNullOrEmpty(analyticsEvent))
            {
#if MODULE_ANALYTICS
                Analytics.TrackEvent(AnalyticsEvents.CurrencySink, new AnalyticsCurrencyData(analyticsEvent, new Dictionary<CurrencyType, int>() { { currencyType, amount } }));
#endif
            }
        }

        public static void SubscribeGlobalCallback(CurrencyCallback currencyChange)
        {
            if (!IsInitialized) { LogNotInitialized(); return; }

            for (int i = 0; i < instance.currencies.Length; i++)
            {
                instance.currencies[i].OnCurrencyChanged += currencyChange;
            }
        }

        public static void UnsubscribeGlobalCallback(CurrencyCallback currencyChange)
        {
            if (!IsInitialized) return;

            for (int i = 0; i < instance.currencies.Length; i++)
            {
                instance.currencies[i].OnCurrencyChanged -= currencyChange;
            }
        }

        private static void LogNotInitialized()
        {
            Debug.LogError("[CurrencyController]: Not initialized. Add CurrencyInitModule to the ProjectInitSettings modules list.");
        }
    }

    public delegate void CurrencyCallback(Currency currency, int difference);
}
