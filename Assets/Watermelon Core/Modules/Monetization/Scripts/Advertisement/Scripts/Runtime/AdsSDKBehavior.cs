using UnityEngine;

namespace Watermelon
{
    public sealed class AdsSDKBehavior : SDKBehavior
    {
        [SerializeField] AdsSettings settings;

        public override void OnUserConsentReceived()
        {
            AdsManager adsManager = new AdsManager();
            adsManager.Init(settings, this);
        }

        private void OnDestroy()
        {
            AdsManager.Unload();
        }
    }
}
