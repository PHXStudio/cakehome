using System.Threading.Tasks;
using UnityEngine;

namespace Watermelon
{
    public class DummyIAPWrapper : IAPWrapper
    {
        public override async Task Init(IAPSettings settings)
        {
            await Task.Yield();

            IAPManager.OnModuleInitialized();
        }

        public override void BuyProduct(ProductKeyType productKeyType)
        {
            if (!IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            IAPItem item = IAPManager.GetIAPItem(productKeyType);

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Payment in progress..");

            Tween.DelayedCall(1.0f, () =>
            {
                LogManager.Log(string.Format("[IAPManager]: Purchasing - {0} is completed!", productKeyType), LogCategory.Services);

                IAPManager.OnPurchaseCompleted(item);

                SystemMessage.ChangeLoadingMessage("Payment complete!");
                SystemMessage.HideLoadingPanel();
            }, unscaledTime: true);
        }

        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
            IAPItem iapItem = IAPManager.GetIAPItem(productKeyType);
            if (iapItem != null)
            {
                return new ProductData(iapItem.ProductType);
            }

            return null;
        }

        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
            return false;
        }

        public override bool IsPurchased(string id)
        {
            return false;
        }

        public override void RestorePurchases()
        {
            // DO NOTHING
        }
    }
}
