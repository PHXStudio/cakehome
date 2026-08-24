using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;

#if MODULE_IAP && !UNITY_IAP_NEW
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
#endif

namespace Watermelon
{
    /// <summary>
    /// Wrapper class for Unity IAP functionality.
    /// </summary>
    public class UnityIAPWrapper : IAPWrapper
#if MODULE_IAP && !UNITY_IAP_NEW
        , IDetailedStoreListener
#endif
    {

#if MODULE_IAP && !UNITY_IAP_NEW
        public static IStoreController Controller { get; private set; }
        public static IExtensionProvider Extensions { get; private set; }

        private List<PurchaseCallback> purchaseCallbacks = new List<PurchaseCallback>();
#endif

        /// <summary>
        /// Initializes the IAP system with the provided settings.
        /// </summary>
        public override async Task Init(IAPSettings settings)
        {
#if MODULE_IAP && !UNITY_IAP_NEW
            try
            {
                var options = new InitializationOptions().SetEnvironmentName("production");

                await UnityServices.InitializeAsync(options);

                StandardPurchasingModule purchasingModule = StandardPurchasingModule.Instance();

                if (settings.UseFakeStore)
                {
                    purchasingModule.useFakeStoreAlways = true;
                    purchasingModule.useFakeStoreUIMode = (FakeStoreUIMode)settings.FakeStoreMode;
                }

                // Initialize products
                ConfigurationBuilder builder = ConfigurationBuilder.Instance(purchasingModule);

                IAPItem[] items = settings.StoreItems;
                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.ID))
                    {
                        builder.AddProduct(item.ID, (UnityEngine.Purchasing.ProductType)item.ProductType);
                    }
                    else
                    {
                        Debug.LogWarning($"[IAP Manager]: Product {item.ProductType} does not have configured IDs.");
                    }
                }

                UnityPurchasing.Initialize(this, builder);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[IAP Manager]: Initialization failed with exception: {exception}");
            }
#else
            await Task.Run(() => LogManager.Log("[IAP Manager]: Define MODULE_IAP is disabled!", LogCategory.Services));
#endif
        }

#if MODULE_IAP && !UNITY_IAP_NEW
        /// <summary>
        /// Called when Unity IAP is successfully initialized.
        /// </summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            UnityIAPWrapper.Controller = controller;
            UnityIAPWrapper.Extensions = extensions;

            IAPManager.OnModuleInitialized();
        }

        /// <summary>
        /// Called when Unity IAP initialization fails.
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            LogManager.Log($"[IAPManager]: Module initialization failed with reason: {error}", LogCategory.Services);
        }

        /// <summary>
        /// Called when Unity IAP initialization fails with a message.
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            LogManager.Log($"[IAPManager]: Module initialization failed with reason: {error}, message: {message}", LogCategory.Services);
        }

        /// <summary>
        /// Processes a successful purchase.
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            Product product = e.purchasedProduct;

            LogManager.Log($"[IAPManager]: Purchasing - {product.definition.id} is completed!", LogCategory.Services);

            IAPItem item = IAPManager.GetIAPItem(product.definition.id);
            if (item != null)
            {
                int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.ProductKeyType);
                if (callbackIndex != -1)
                {
                    PurchaseCallback callback = purchaseCallbacks[callbackIndex];

                    callback.Callback?.Invoke(new AnalyticsIAPData()
                    {
                        Item = item,

                        Receipt = product.receipt,
                        StoreSpecificId = product.definition.storeSpecificId,
                        IsoCurrencyCode = product.metadata.isoCurrencyCode,
                        LocalizedPrice = (float)product.metadata.localizedPrice,
                    });

                    purchaseCallbacks.RemoveAt(callbackIndex);
                }

                IAPManager.OnPurchaseCompleted(item);
            }
            else
            {
                LogManager.Log($"[IAPManager]: Product - {product.definition.id} can't be found!", LogCategory.Services);
            }

            SystemMessage.ChangeLoadingMessage("Payment complete!");
            SystemMessage.HideLoadingPanel();

            return PurchaseProcessingResult.Complete;
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, UnityEngine.Purchasing.PurchaseFailureReason failureReason)
        {
            LogManager.Log($"[IAPManager]: Purchasing - {product.definition.id} failed with reason: {failureReason}", LogCategory.Services);

            IAPItem item = IAPManager.GetIAPItem(product.definition.id);
            if (item != null)
            {
                int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.ProductKeyType);
                if (callbackIndex != -1)
                    purchaseCallbacks.RemoveAt(callbackIndex);

                Watermelon.PurchaseFailureReason purchaseFailureReason = (Watermelon.PurchaseFailureReason)failureReason;

                AnalyticsController.OnIAPFailed(item, purchaseFailureReason);

                IAPManager.OnPurchaseFailed(item.ProductKeyType, purchaseFailureReason);
            }
            else
            {
                LogManager.Log($"[IAPManager]: Product - {product.definition.id} can't be found!", LogCategory.Services);
            }

            SystemMessage.ChangeLoadingMessage("Payment failed!");
            SystemMessage.HideLoadingPanel();
        }

        /// <summary>
        /// Called when a purchase fails with a description.
        /// </summary>
        public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, PurchaseFailureDescription failureDescription)
        {
            LogManager.Log($"[IAPManager]: Purchasing - {product.definition.id} failed with reason: {failureDescription.message}", LogCategory.Services);

            IAPItem item = IAPManager.GetIAPItem(product.definition.id);
            if (item != null)
            {
                int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.ProductKeyType);
                if (callbackIndex != -1)
                    purchaseCallbacks.RemoveAt(callbackIndex);

                Watermelon.PurchaseFailureReason purchaseFailureReason = (Watermelon.PurchaseFailureReason)failureDescription.reason;

                AnalyticsController.OnIAPFailed(item, purchaseFailureReason);

                IAPManager.OnPurchaseFailed(item.ProductKeyType, purchaseFailureReason);
            }
            else
            {
                LogManager.Log($"[IAPManager]: Product - {product.definition.id} can't be found!", LogCategory.Services);
            }

            SystemMessage.ChangeLoadingMessage("Payment failed!");
            SystemMessage.HideLoadingPanel();
        }
#endif

        /// <summary>
        /// Restores previously purchased products.
        /// </summary>
        public override void RestorePurchases()
        {
#if MODULE_IAP && !UNITY_IAP_NEW
            if (!IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Restoring purchased products..");

#if UNITY_ANDROID
            Extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions(OnRestored);
#elif UNITY_IOS
            Extensions.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestored);
#endif
#endif

#if UNITY_EDITOR
            OnRestored(true, "");
#endif
        }

        private void OnRestored(bool result, string error)
        {
#if MODULE_IAP && !UNITY_IAP_NEW
            Tween.DelayedCall(0.5f, () =>
            {
                if (result)
                {
                    SystemMessage.ChangeLoadingMessage("Restoration completed!");
                }
                else
                {
                    SystemMessage.ChangeLoadingMessage($"Restoration failed with error: {error}!");
                }

                Tween.DelayedCall(0.5f, () =>
                {
                    SystemMessage.HideLoadingPanel();
                }, unscaledTime: true);
            }, unscaledTime: true);
#endif
        }

        /// <summary>
        /// Initiates the purchase of a product.
        /// </summary>
        public override void BuyProduct(ProductKeyType productKeyType)
        {
#if MODULE_IAP && !UNITY_IAP_NEW
            if (!IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Payment in progress..");

            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                AnalyticsController.OnIAPClicked(item);

                for (int i = purchaseCallbacks.Count - 1; i >= 0; i--)
                {
                    if (purchaseCallbacks[i].ProductKeyType == productKeyType)
                    {
                        purchaseCallbacks.RemoveAt(i);
                    }
                }

                purchaseCallbacks.Add(new PurchaseCallback(productKeyType, (iapData) =>
                {
                    AnalyticsController.OnIAPPurchased(iapData);
                }));

                Controller.InitiatePurchase(item.ID);
            }
#else
            SystemMessage.ShowMessage("Network error.");
#endif
        }

        /// <summary>
        /// Gets the product data for a specified product key type.
        /// </summary>
        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
            if (!IAPManager.IsInitialized)
                return null;

#if MODULE_IAP && !UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                return new ProductData(Controller.products.WithID(item.ID));
            }
#endif

            return null;
        }

        /// <summary>
        /// Checks if a product is subscribed.
        /// </summary>
        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
#if MODULE_IAP && !UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                Product product = Controller.products.WithID(item.ID);
                if (product != null)
                {
                    if (product.receipt == null)
                        return false;

                    SubscriptionManager subscriptionManager = new SubscriptionManager(product, null);
                    SubscriptionInfo info = subscriptionManager.getSubscriptionInfo();

                    return info.isSubscribed() == Result.True;
                }
            }
#endif

            return false;
        }

        public override bool IsPurchased(string id)
        {
#if MODULE_IAP && !UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItem(id);
            if (item != null)
            {
                Product product = Controller.products.WithID(item.ID);
                if (product != null)
                {
                    return product.hasReceipt;
                }
            }
#endif

            return false;
        }

#if MODULE_IAP && !UNITY_IAP_NEW
        private class PurchaseCallback
        {
            public ProductKeyType ProductKeyType { get; private set; }
            public PurchaseCallbackDelegate Callback { get; private set; }

            public PurchaseCallback(ProductKeyType productKeyType, PurchaseCallbackDelegate callback)
            {
                ProductKeyType = productKeyType;
                Callback = callback;
            }

            public delegate void PurchaseCallbackDelegate(AnalyticsIAPData iapData);
        }
#endif
    }
}
