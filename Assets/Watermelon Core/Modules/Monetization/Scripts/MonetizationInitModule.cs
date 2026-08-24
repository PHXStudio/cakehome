using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Monetization")]
    public class MonetizationInitModule : InitModule
    {
        public override string ModuleName => "Monetization";

        [SerializeField] MonetizationSettings settings;

        public override IEnumerator InitAsync(GameObject owner)
        {
            Monetization.Init(settings);

            AdsManager adsManager = new AdsManager();
            adsManager.Init(settings.AdsSettings, owner.GetComponent<Initializer>());

            IAPManager iapManager = owner.AddComponent<IAPManager>();
            iapManager.Init(settings.IAPSettings);

            yield break;
        }
    }
}
