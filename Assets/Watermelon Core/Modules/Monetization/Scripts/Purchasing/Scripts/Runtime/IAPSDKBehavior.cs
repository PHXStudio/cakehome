using UnityEngine;

namespace Watermelon
{
    public sealed class IAPSDKBehavior : SDKBehavior
    {
        [SerializeField] IAPSettings settings;

        private IAPManager iapManager;

        public override void OnUserConsentReceived()
        {
            // IAP module can already be initialized by MonetizationInitModule — skip the duplicate init
            if (IAPManager.IsInitialized) return;

            iapManager = gameObject.AddComponent<IAPManager>();
            iapManager.Init(settings);
        }

        private void OnDestroy()
        {
            iapManager?.Unload();
            iapManager = null;
        }
    }
}
