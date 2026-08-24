using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;

#if MODULE_IAP && UNITY_IAP_NEW
using UnityEngine.Purchasing;
// Unity.Purchasing.Security's own asmdef excludes Editor entirely and only
// includes Android/iOS player builds — mirror that here so this file compiles
// everywhere else (Editor, Standalone, WebGL, consoles).
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using UnityEngine.Purchasing.Security;
#endif
#endif

namespace Watermelon
{
    /// <summary>
    /// Wrapper class for Unity IAP functionality.
    /// </summary>
    public class UnityIAP5Wrapper : IAPWrapper
    {
        private readonly StoreType CURRENT_STORE = GetCurrentStore();

#if MODULE_IAP && UNITY_IAP_NEW
        private StoreController m_StoreController;

        private List<PurchaseCallback> purchaseCallbacks = new List<PurchaseCallback>();

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        private CrossPlatformValidator m_Validator = null;
#endif

        private EntitlementsService entitlementsService;
#endif

        private FraudDetectionData fraudDetectionData;

        /// <summary>
        /// Initializes the IAP system with the provided settings.
        /// </summary>
        public override async Task Init(IAPSettings settings)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            try
            {
                m_StoreController = UnityIAPServices.StoreController();

                m_StoreController.OnPurchasePending += OnPurchasePending;
                m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

                m_StoreController.OnStoreConnected += OnStoreConnected;
                m_StoreController.OnStoreDisconnected += OnStoreDisconnected;

                LogManager.Log("[IAP Manager]: Connecting to store.", LogCategory.Services);

                await m_StoreController.Connect();

                fraudDetectionData = new FraudDetectionData();
                entitlementsService = new EntitlementsService(m_StoreController);

#if UNITY_ANDROID
                ConfigureGoogleFraudDetection(m_StoreController.GooglePlayStoreExtendedService);
#elif UNITY_IOS
                ConfigureAppleFraudDetection(m_StoreController.AppleStoreExtendedService);
#endif

                ConfigureValidator();

                m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
                m_StoreController.OnProductsFetched += OnProductsFetched;
                m_StoreController.OnPurchasesFetched += OnPurchasesFetched;
                m_StoreController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                m_StoreController.OnPurchaseDeferred += OnPurchaseDeferred;

                List<ProductDefinition> initialProductsToFetch = new List<ProductDefinition>();

                IAPItem[] items = settings.StoreItems;
                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.ID))
                    {
                        initialProductsToFetch.Add(new ProductDefinition(item.ID, (UnityEngine.Purchasing.ProductType)item.ProductType));
                    }
                    else
                    {
                        Debug.LogWarning($"[IAP Manager]: Product {item.ProductType} does not have configured IDs.");
                    }
                }

                m_StoreController.FetchProducts(initialProductsToFetch);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[IAP Manager]: Initialization failed with exception: {exception}");
            }
#else
            await Task.Run(() => LogManager.Log("[IAP Manager]: Define MODULE_IAP is disabled!", LogCategory.Services));
#endif
        }

#if MODULE_IAP && UNITY_IAP_NEW
        private void OnPurchaseDeferred(DeferredOrder order)
        {
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    LogManager.Log("[IAPManager]: Could not find product in order.", LogCategory.Services);
                    continue;
                }

                LogManager.Log($"OnPurchaseDeferred - Product: {product?.definition.id}", LogCategory.Services);
            }
        }
#endif

        private void ConfigureValidator()
        {
            if (CURRENT_STORE != StoreType.GooglePlay) return;

#if MODULE_IAP && UNITY_IAP_NEW
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Type googlePlayTangleType = Type.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle");
            if (googlePlayTangleType != null)
            {
                MethodInfo dataMethod = googlePlayTangleType.GetMethod("Data", BindingFlags.Static | BindingFlags.Public);
                if (dataMethod != null)
                {
                    byte[] googleData = (byte[])dataMethod.Invoke(null, null);
                    m_Validator = new CrossPlatformValidator(googleData, Application.identifier);
                }
            }
#endif
#endif
        }

#if MODULE_IAP && UNITY_IAP_NEW
        private void OnPurchaseFailed(FailedOrder order)
        {
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    LogManager.Log("[IAPManager]: Could not find product in order.", LogCategory.Services);
                    continue;
                }

                LogManager.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                          $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                          + $"Confirmation Failure Details: {order.Details}", LogCategory.Services);

                IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                if (item != null)
                {
                    int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.ProductKeyType);
                    if (callbackIndex != -1)
                        purchaseCallbacks.RemoveAt(callbackIndex);

                    Watermelon.PurchaseFailureReason purchaseFailureReason = (Watermelon.PurchaseFailureReason)order.FailureReason;

#if MODULE_ANALYTICS
                    AnalyticsController.OnIAPFailed(item, purchaseFailureReason);
#endif

                    IAPManager.OnPurchaseFailed(item.ProductKeyType, purchaseFailureReason);
                }
                else
                {
                    LogManager.Log($"[IAPManager]: Product - {product.definition.id} can't be found!", LogCategory.Services);
                }

                LogManager.LogWarning($"Purchase failed - Product: '{product?.definition.id}'," +
                          $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                          + $"Purchase Failure Details: {order.Details}", LogCategory.Services);
            }

            SystemMessage.ChangeLoadingMessage("Payment failed!");
            SystemMessage.HideLoadingPanel();
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        private bool IsPurchaseValid(Order order)
        {
            //If the validator doesn't support the current store, we assume the purchase is valid
            if (m_Validator != null)
            {
                try
                {
                    IPurchaseReceipt[] result = m_Validator.Validate(order.Info.Receipt);

                    LogManager.Log("Receipt is valid. Contents:", LogCategory.Services);
                    foreach (var receipt in result)
                    {
                        LogManager.Log($"Product ID: {receipt.productID}\n" +
                            $"Purchase Date: {receipt.purchaseDate}\n" +
                            $"Transaction ID: {receipt.transactionID}", LogCategory.Services);

                        if (receipt is GooglePlayReceipt googleReceipt)
                        {
                            LogManager.Log($"Purchase State: {googleReceipt.purchaseState}\n" +
                                $"Purchase Token: {googleReceipt.purchaseToken}", LogCategory.Services);
                        }
                    }
                }
                //If the purchase is deemed invalid, the validator throws an IAPSecurityException.
                catch (IAPSecurityException reason)
                {
                    LogManager.Log($"Invalid receipt: {reason}", LogCategory.Services);

                    return false;
                }
            }

            return true;
        }
#endif

        private void OnPurchasePending(PendingOrder order)
        {
            // Presume valid for platforms with no receipt validator.
            bool validPurchase = true;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            // Unity IAP's validation logic is only included on these platforms.
            if (m_Validator != null)
            {
                validPurchase = IsPurchaseValid(order);
            }
#endif

            if (validPurchase)
            {
                // We call CompletePurchase, informing Unity IAP that the processing on our side is done and the transaction can be closed.
                m_StoreController.ConfirmPurchase(order);
            }
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription description)
        {

        }

        private void OnPurchasesFetched(Orders orders)
        {
            HashSet<string> ownedProductIds = new HashSet<string>();

            foreach (ConfirmedOrder confirmedOrder in orders.ConfirmedOrders)
            {
                foreach (CartItem item in confirmedOrder.CartOrdered.Items())
                {
                    string productId = item.Product.definition.id;

                    ownedProductIds.Add(productId);
                }
            }

            foreach (string productId in ownedProductIds)
            {
                IAPItem iapItem = IAPManager.GetIAPItem(productId);
                if (iapItem == null)
                    continue;

                if (iapItem.ProductType == ProductType.Consumable)
                    continue;

                iapItem.OnProductRestored();
            }

            SaveController.MarkAsSaveIsRequired();
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
                case FailedOrder failedOrder:
                    OnPurchaseConfirmationFailed(failedOrder);
                    break;
                default:
                    LogManager.Log("Unknown OnPurchaseConfirmed result.", LogCategory.Services);
                    break;
            }
        }

        private void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    LogManager.Log("[IAPManager]: Could not find product in order.", LogCategory.Services);
                    continue;
                }

                LogManager.Log($"[IAPManager]: Purchasing - {product.definition.id} is completed!", LogCategory.Services);

                IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                if (item != null)
                {
                    int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.ProductKeyType);
                    if (callbackIndex != -1)
                    {
                        PurchaseCallback callback = purchaseCallbacks[callbackIndex];

                        LogManager.Log("[IAPManager]: IAP Analytics Callback", LogCategory.Services);

                        callback.Callback?.Invoke(new AnalyticsIAPData()
                        {
                            Item = item,

                            Receipt = order.Info.Receipt,

                            IsoCurrencyCode = product.metadata.isoCurrencyCode,
                            LocalizedPrice = (float)product.metadata.localizedPrice,
                            StoreSpecificId = product.definition.storeSpecificId
                        });

                        purchaseCallbacks.RemoveAt(callbackIndex);
                    }

                    IAPManager.OnPurchaseCompleted(item);
                }
                else
                {
                    LogManager.LogWarning($"[IAPManager]: Product - {product.definition.id} can't be found!", LogCategory.Services);
                }
            }

            SystemMessage.ChangeLoadingMessage("Payment complete!");
            SystemMessage.HideLoadingPanel();
        }

        private void OnPurchaseConfirmationFailed(FailedOrder order)
        {
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    LogManager.Log("[IAPManager]: Could not find product in order.", LogCategory.Services);
                    continue;
                }

                LogManager.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                          $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                          + $"Confirmation Failure Details: {order.Details}", LogCategory.Services);
            }

            SystemMessage.ChangeLoadingMessage("Purchase failed!");
            SystemMessage.HideLoadingPanel();
        }

        // Calling StoreController.Connect without a listener on the StoreController.OnStoreConnected event will result in warnings.
        private void OnStoreConnected()
        {
            LogManager.Log("[IAP Manager]: Store connected.", LogCategory.Services);
        }

        // Calling StoreController.Connect without a listener on the StoreController.OnStoreDisconnected event will result in warnings.
        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            LogManager.Log($"Store disconnected details: {description.message}", LogCategory.Services);
        }

        // Calling StoreController.Connect without listeners on StoreController.OnProductsFetched and StoreController.OnProductsFetchedFailed will result in warnings.
        private void OnProductsFetched(List<Product> products)
        {
            LogManager.Log($"Products fetched successfully for {products.Count} products.", LogCategory.Services);

            m_StoreController.FetchPurchases();

            IAPManager.OnModuleInitialized();
        }

        private void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            LogManager.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}", LogCategory.Services);
        }
#endif

        /// <summary>
        /// Restores previously purchased products.
        /// </summary>
        public override void RestorePurchases()
        {
#if MODULE_IAP && UNITY_IAP_NEW
            if (!IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Restoring purchased products..");

            m_StoreController.RestoreTransactions(OnRestored);
#endif

#if UNITY_EDITOR
            OnRestored(true, "");
#endif
        }

        private void OnRestored(bool result, string error)
        {
#if MODULE_IAP && UNITY_IAP_NEW
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
#if MODULE_IAP && UNITY_IAP_NEW
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
#if MODULE_ANALYTICS
                AnalyticsController.OnIAPClicked(item);
#endif

                for (int i = purchaseCallbacks.Count - 1; i >= 0; i--)
                {
                    if (purchaseCallbacks[i].ProductKeyType == productKeyType)
                    {
                        purchaseCallbacks.RemoveAt(i);
                    }
                }

                purchaseCallbacks.Add(new PurchaseCallback(productKeyType, (analyticsData) =>
                {
#if MODULE_ANALYTICS
                    AnalyticsController.OnIAPPurchased(analyticsData);
#endif
                }));

                m_StoreController.PurchaseProduct(item.ID);
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

#if MODULE_IAP && UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                return new ProductData(m_StoreController.GetProductById(item.ID));
            }
#endif

            return null;
        }

        /// <summary>
        /// Checks if a product is subscribed.
        /// </summary>
        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item != null)
            {
                return entitlementsService.IsSubscriptionActive(item.ID);
            }
#endif

            return false;
        }

        public override bool IsPurchased(string id)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            return entitlementsService.IsOwned(id);
#else
            return false;
#endif
        }

#if MODULE_IAP && UNITY_IAP_NEW
        private void ConfigureGoogleFraudDetection(IGooglePlayStoreExtendedService googlePlayStoreExtendedService)
        {
            if (googlePlayStoreExtendedService == null)
            {
                LogManager.Log("Google Play Store Extended Service is not available. Please make sure the project is being built for Android and the Google Play Store.", LogCategory.Services);
                return;
            }

            googlePlayStoreExtendedService.SetObfuscatedAccountId(fraudDetectionData.AccountId);
        }

        private void ConfigureAppleFraudDetection(IAppleStoreExtendedService appleStoreExtendedService)
        {
            if (appleStoreExtendedService == null)
            {
                LogManager.Log("App Store Extended Service is not available. Please make sure the project is being built for Android and the Google Play Store.", LogCategory.Services);
                return;
            }

            appleStoreExtendedService.SetAppAccountToken(fraudDetectionData.AccountToken);
        }
#endif

        private static StoreType GetCurrentStore()
        {
#if UNITY_EDITOR
            return StoreType.Fake;
#elif UNITY_ANDROID
            return StoreType.GooglePlay;
#elif UNITY_IOS
            return StoreType.AppleAppStore;
#elif UNITY_STANDALONE_OSX
            return StoreType.MacAppStore;
#else
            return StoreType.NotSpecified;
#endif
        }

        public enum StoreType
        {
            NotSpecified = 0,
            Fake = 1,
            GooglePlay = 2,
            MacAppStore = 3,
            AppleAppStore = 4
        }

        [System.Serializable]
        public class FraudDetectionData
        {
            public string AccountId;
            [System.NonSerialized] public Guid AccountToken;

            public FraudDetectionData()
            {
                AccountId = User.GetIdSha256Hex();
                AccountToken = User.GetIdAsGuidFromSha256();
            }
        }

#if MODULE_IAP && UNITY_IAP_NEW
        public sealed class EntitlementsService
        {
            private StoreController store;

            // Non-consumables you own (Unity product ids).
            private readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);

            // Subscriptions by Unity product id.
            private readonly Dictionary<string, SubscriptionStatusSnapshot> subs = new Dictionary<string, SubscriptionStatusSnapshot>(StringComparer.Ordinal);

            // Last known entitlement status per Unity product id (from CheckEntitlement).
            private readonly Dictionary<string, EntitlementStatus> entitlements = new Dictionary<string, EntitlementStatus>(StringComparer.Ordinal);

            public event Action EntitlementsChanged;

            public EntitlementsService(StoreController store)
            {
                this.store = store ?? throw new ArgumentNullException(nameof(store));

                this.store.OnPurchasesFetched += OnPurchasesFetched;
                this.store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                this.store.OnCheckEntitlement += OnCheckEntitlement;

                this.store.ProcessPendingOrdersOnPurchasesFetched(true);
            }

            public void RefreshAll() => store?.FetchPurchases();
            public bool IsOwned(string unityProductId) => owned.Contains(unityProductId);
            public bool IsSubscriptionActive(string unityProductId) => subs.TryGetValue(unityProductId, out var s) && s.IsActive;
            public SubscriptionStatusSnapshot GetSubscription(string unityProductId) => subs.TryGetValue(unityProductId, out var s) ? s : default;
            public EntitlementStatus GetEntitlementStatus(string unityProductId) => entitlements.TryGetValue(unityProductId, out var st) ? st : EntitlementStatus.Unknown;

            public IReadOnlyCollection<string> OwnedNonConsumables => new ReadOnlyCollection<string>(owned.ToList());
            public IReadOnlyDictionary<string, SubscriptionStatusSnapshot> Subscriptions => new ReadOnlyDictionary<string, SubscriptionStatusSnapshot>(subs);

            private void OnPurchasesFetched(Orders orders)
            {
                owned.Clear();
                subs.Clear();

                if (orders == null)
                {
                    RaiseChanged();
                    return;
                }

                foreach (ConfirmedOrder confirmed in orders.ConfirmedOrders)
                    AddOrder(confirmed);

                RaiseChanged();
            }

            private void OnPurchaseConfirmed(Order order)
            {
                if (order is ConfirmedOrder confirmed)
                {
                    AddOrder(confirmed);
                    RaiseChanged();
                }
            }

            private void OnCheckEntitlement(Entitlement entitlement)
            {
                if (entitlement?.Product == null) return;

                string unityId = entitlement.Product.definition?.id;
                if (string.IsNullOrEmpty(unityId)) return;

                entitlements[unityId] = entitlement.Status;

                if (entitlement.Order is ConfirmedOrder co)
                {
                    AddOrder(co);
                    RaiseChanged();
                }
            }

            public bool RequestEntitlementCheck(string unityProductId)
            {
                Product p = store?.GetProductById(unityProductId);
                if (p == null) return false;

                store.CheckEntitlement(p);
                return true;
            }

            private void AddOrder(ConfirmedOrder confirmed)
            {
                if (confirmed?.Info?.PurchasedProductInfo == null)
                    return;

                foreach (IPurchasedProductInfo ppi in confirmed.Info.PurchasedProductInfo)
                {
                    string unityId = ResolveUnityProductId(ppi);
                    if (unityId == null)
                        continue;

                    Product product = store.GetProductById(unityId);
                    UnityEngine.Purchasing.ProductType pType = product?.definition?.type ?? UnityEngine.Purchasing.ProductType.Unknown;

                    switch (pType)
                    {
                        case UnityEngine.Purchasing.ProductType.NonConsumable:
                            owned.Add(unityId);
                            break;

                        case UnityEngine.Purchasing.ProductType.Subscription:
                            SubscriptionStatusSnapshot snap = BuildSubscriptionSnapshot(unityId, ppi);
                            subs[unityId] = snap;
                            break;
                    }
                }
            }

            /// <summary>
            /// In different stores, the productId in the receipt may contain either the Unity ID or the store-specific ID.
            /// We make several attempts to match it with the catalog.
            /// </summary>
            private string ResolveUnityProductId(IPurchasedProductInfo ppi)
            {
                if (ppi == null) return null;

                // 1) Direct attempt: is this already the Unity ID?
                if (!string.IsNullOrEmpty(ppi.productId) && store.GetProductById(ppi.productId) != null)
                    return ppi.productId;

                // 2) Attempt using the storeSpecificId.
                ReadOnlyObservableCollection<Product> all = store.GetProducts();
                Product fromStoreSpecific = all.FirstOrDefault(p => string.Equals(p.definition?.storeSpecificId, ppi.productId, StringComparison.OrdinalIgnoreCase));

                if (fromStoreSpecific != null)
                    return fromStoreSpecific.definition.id;

                // 3) For subscriptions: extract the store ID from SubscriptionInfo and match it.
                string storePid = ppi.subscriptionInfo?.GetProductId();
                if (!string.IsNullOrEmpty(storePid))
                {
                    Product bySubStoreId = all.FirstOrDefault(p => string.Equals(p.definition?.storeSpecificId, storePid, StringComparison.OrdinalIgnoreCase));

                    if (bySubStoreId != null)
                        return bySubStoreId.definition.id;
                }

                LogManager.LogWarning($"[IapEntitlements] Unknown purchased product id: '{ppi.productId}'. Ensure your catalog IDs match store product IDs.", LogCategory.Services);

                return null;
            }

            private static SubscriptionStatusSnapshot BuildSubscriptionSnapshot(string unityProductId, IPurchasedProductInfo ppi)
            {
                SubscriptionInfo si = ppi.subscriptionInfo;
                if (si == null)
                    return new SubscriptionStatusSnapshot(unityProductId, false, null, null, false, false, null, null, null);

                Result expired = si.IsExpired();
                bool isActive = expired == Result.False;

                DateTime? expireAt = null;
                try { expireAt = si.GetExpireDate(); }
                catch { /* platform may not provide */ }

                TimeSpan? remaining = null;
                try { remaining = si.GetRemainingTime(); }
                catch { }

                bool autoRenews = false;
                try { autoRenews = si.IsAutoRenewing() == Result.True; }
                catch { }

                bool cancelled = false;
                try { cancelled = si.IsCancelled() == Result.True; }
                catch { }

                DateTime? purchaseDate = null;
                try { purchaseDate = si.GetPurchaseDate(); }
                catch { }

                string storeProductId = null;
                try { storeProductId = si.GetProductId(); }
                catch { }

                string payload = null;
                try { payload = si.GetSubscriptionInfoJsonString(); }
                catch { }

                return new SubscriptionStatusSnapshot(
                    unityProductId,
                    isActive,
                    expireAt,
                    remaining,
                    autoRenews,
                    cancelled,
                    storeProductId,
                    purchaseDate,
                    payload
                );
            }

            private void RaiseChanged() => EntitlementsChanged?.Invoke();
        }

        /// <summary>
        /// Immutable snapshot of a subscription state. Safe to cache or serialize to diagnostics.
        /// </summary>
        public readonly struct SubscriptionStatusSnapshot
        {
            public readonly string UnityProductId;
            public readonly bool IsActive;
            public readonly DateTime? ExpireAtUtc;
            public readonly TimeSpan? Remaining;
            public readonly bool AutoRenews;
            public readonly bool IsCancelled;
            public readonly string StoreProductId;
            public readonly DateTime? PurchaseDateUtc;
            public readonly string RawJson;

            public SubscriptionStatusSnapshot(string unityProductId, bool isActive, DateTime? expireAtUtc, TimeSpan? remaining, bool autoRenews, bool isCancelled, string storeProductId, DateTime? purchaseDateUtc, string rawJson)
            {
                UnityProductId = unityProductId;
                IsActive = isActive;
                ExpireAtUtc = expireAtUtc;
                Remaining = remaining;
                AutoRenews = autoRenews;
                IsCancelled = isCancelled;
                StoreProductId = storeProductId;
                PurchaseDateUtc = purchaseDateUtc;
                RawJson = rawJson;
            }

            public override string ToString() => $"[{UnityProductId}] Active={IsActive}, Expire={ExpireAtUtc:O}, AutoRenews={AutoRenews}, Cancelled={IsCancelled}";
        }

        private class PurchaseCallback
        {
            public ProductKeyType ProductKeyType { get; private set; }
            public PurchaseCallbackDelegate Callback { get; private set; }

            public PurchaseCallback(ProductKeyType productKeyType, PurchaseCallbackDelegate callback)
            {
                ProductKeyType = productKeyType;
                Callback = callback;
            }

            public delegate void PurchaseCallbackDelegate(AnalyticsIAPData analyticsIAPData);
        }
#endif
    }
}
