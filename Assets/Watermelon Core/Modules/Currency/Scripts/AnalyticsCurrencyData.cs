using System.Collections.Generic;

namespace Watermelon
{
    public class AnalyticsCurrencyData : IAnalyticsData
    {
        public string Source;
        public Dictionary<CurrencyType, int> CurrenciesDelta;

        public AnalyticsCurrencyData(string source, Dictionary<CurrencyType, int> currenciesDelta)
        {
            Source = source;
            CurrenciesDelta = currenciesDelta;
        }
    }
}
