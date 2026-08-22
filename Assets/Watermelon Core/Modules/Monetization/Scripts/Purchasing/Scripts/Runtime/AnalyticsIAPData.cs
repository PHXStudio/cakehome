namespace Watermelon
{
    public class AnalyticsIAPData : IAnalyticsData
    {
        public IAPItem Item;

        public string Receipt;
        public float LocalizedPrice;
        public string StoreSpecificId;
        public string IsoCurrencyCode;
    }
}
