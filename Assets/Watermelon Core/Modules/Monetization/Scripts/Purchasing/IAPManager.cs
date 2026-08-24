using System.Collections.Generic;
using UnityEngine;

#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace Watermelon
{
    public class IAPManager : MonoBehaviour
    {
        // IAP
        public const string ANALYTICS_IAP_CLICKED = "iap_clicked";
        public const string ANALYTICS_IAP_PURCHASED = "iap_purchased";
        public const string ANALYTICS_IAP_FAILED = "iap_failed";
        public const string ANALYTICS_IAP_FIRST_PURCHASE = "iap_first_purchase";

        private static IAPManager instance;

        private static Dictionary<ProductKeyType, IAPItem> productsTypeToProductLink;
        private static Dictionary<string, IAPItem> productsIDToProductLink;

        private static bool isInitialized;
        public static bool IsInitialized => isInitialized;

        private static IAPWrapper wrapper;

        public static event SimpleCallback Initialized;
        public static event ProductCallback PurchaseCompleted;
        public static event ProductFailCallback PurchaseFailed;

        private static IAPSettings settings;
        private static Save save;

        // Static facade

        public static IAPItem GetIAPItem(string productID)
        {
            if (string.IsNullOrEmpty(productID)) return null;

            productsIDToProductLink.TryGetValue(productID, out IAPItem item);
            return item;
        }

        public static IAPItem GetIAPItem(ProductKeyType productKeyType)
        {
            productsTypeToProductLink.TryGetValue(productKeyType, out IAPItem item);
            return item;
        }

        public static void RestorePurchases()
        {
            if (!isInitialized) return;

            wrapper.RestorePurchases();
        }

        public static void SubscribeOnPurchaseModuleInitted(SimpleCallback callback)
        {
            if (isInitialized)
                callback?.Invoke();
            else
                Initialized += callback;
        }

        public static void BuyProduct(ProductKeyType productKeyType)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[IAP Manager]: The module is not initialized!");
                return;
            }

            wrapper.BuyProduct(productKeyType);
        }

        public static ProductData GetProductData(ProductKeyType productKeyType)
        {
            if (!isInitialized) return new ProductData();

            ProductData product = wrapper.GetProductData(productKeyType);

            if (product == null)
            {
                Debug.LogWarning($"[IAP Manager]: Product of type '{productKeyType}' was not found in IAP Settings. Please ensure it is added to the products list.");
            }

            return product;
        }

        public static bool IsSubscribed(ProductKeyType productKeyType)
        {
            if (!isInitialized) return false;

            return wrapper.IsSubscribed(productKeyType);
        }

        public static bool IsPurchased(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            IAPItem iapItem = GetIAPItem(productKeyType);
            if (iapItem != null)
                return wrapper.IsPurchased(iapItem.ID);
#endif
            return false;
        }

        public static string GetProductLocalPriceString(ProductKeyType productKeyType)
        {
            ProductData product = GetProductData(productKeyType);

            if (product == null)
            {
                Debug.LogWarning($"[IAP Manager]: Product of type '{productKeyType}' was not found in IAP Settings. Please ensure it is added to the products list.");
                return string.Empty;
            }

            return $"{product.ISOCurrencyCode} {product.Price}";
        }

        public static bool IsPayableUser() => save != null && save.FirstPurchase;

        // Instance methods

        public void Init(IAPSettings iapSettings)
        {
            if (isInitialized)
            {
                Debug.LogError("[IAP Manager]: Module is already initialized!");
                return;
            }

            if (iapSettings == null)
            {
                Debug.LogError("[IAP Manager]: IAPSettings is null!");
                return;
            }

            instance = this;
            settings = iapSettings;

            save = SaveController.GetSaveObject<Save>("iapGlobalSave");

            productsTypeToProductLink = new Dictionary<ProductKeyType, IAPItem>();
            productsIDToProductLink = new Dictionary<string, IAPItem>();

#if MODULE_REMOTE_CONFIG
            IAPRemoteConfigData remoteConfigData = RemoteConfigController.TryGetConfig<IAPRemoteConfigData>("iaps");
#endif

            IAPItem[] items = settings.StoreItems;
            if (items != null)
            {
                foreach (IAPItem item in items)
                {
                    item.Init();

#if MODULE_REMOTE_CONFIG
                    IAPRemoteConfigData.IAP remoteConfigItem = remoteConfigData?.GetOverride(item.ID);
                    item.OverrideDefaultPrice(remoteConfigItem);
#endif

                    if (!productsTypeToProductLink.ContainsKey(item.ProductKeyType))
                    {
                        productsTypeToProductLink.Add(item.ProductKeyType, item);
                        productsIDToProductLink[item.ID] = item;
                    }
                    else
                    {
                        Debug.LogError($"[IAP Manager]: Product with the type {item.ProductKeyType} has duplicates in the list!");
                    }
                }
            }

            wrapper = GetPlatformWrapper();
            _ = wrapper.Init(settings);
        }

        // Wrapper callbacks (static — wrappers call via IAPManager.X)

        public static void OnModuleInitialized()
        {
            isInitialized = true;

            if (Initialized != null)
            {
                System.Delegate[] listDelegates = Initialized.GetInvocationList();
                foreach (var d in listDelegates)
                {
                    SimpleCallback cb = (SimpleCallback)d;

                    if (d.Target is UnityEngine.Object uo && uo == null)
                        continue;

                    try
                    {
                        cb?.Invoke();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            
            LogManager.Log("[IAPManager]: Module is initialized!", LogCategory.Services);
        }

        public static void OnPurchaseCompleted(IAPItem item)
        {
            if (!save.FirstPurchase)
            {
#if MODULE_ANALYTICS
                Analytics.TrackEvent(ANALYTICS_IAP_FIRST_PURCHASE);
#endif

                save.FirstPurchase = true;
            }

            item.OnProductPurchased();

            PurchaseCompleted?.Invoke(item.ProductKeyType);
        }

        public static void OnPurchaseFailed(ProductKeyType productKey, Watermelon.PurchaseFailureReason failureReason)
        {
            PurchaseFailed?.Invoke(productKey, failureReason);
        }

        public void Unload()
        {
            isInitialized = false;

            Initialized = null;
            PurchaseCompleted = null;
            PurchaseFailed = null;

            productsTypeToProductLink = null;
            productsIDToProductLink = null;
            wrapper = null;
            settings = null;
            save = null;

            instance = null;
        }

        private static IAPWrapper GetPlatformWrapper()
        {
#if MODULE_IAP
#if UNITY_IAP_NEW
            return new UnityIAP5Wrapper();
#else
            return new UnityIAPWrapper();
#endif
#else
            return new DummyIAPWrapper();
#endif
        }

        public delegate void ProductCallback(ProductKeyType productKeyType);
        public delegate void ProductFailCallback(ProductKeyType productKeyType, Watermelon.PurchaseFailureReason failureReason);

        [System.Serializable]
        public class Save : ISaveObject
        {
            public bool FirstPurchase = false;

            public void OnBeforeSave() { }
        }
    }
}
