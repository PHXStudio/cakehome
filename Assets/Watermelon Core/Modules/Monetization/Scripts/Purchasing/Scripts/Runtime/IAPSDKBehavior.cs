using UnityEngine;

namespace Watermelon
{
    public sealed class IAPSDKBehavior : SDKBehavior
    {
        [SerializeField] IAPSettings settings;

        private IAPManager iapManager;

        public override void OnUserConsentReceived()
        {
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
