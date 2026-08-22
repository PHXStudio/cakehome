using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class ConsentData
    {
        private static ConsentData instance;

        private readonly List<IConsentProvider> providers = new List<IConsentProvider>();

        public ConsentData(GameObject sdkRoot)
        {
            instance = this;

            if (sdkRoot != null)
            {
                IConsentProvider[] found = sdkRoot.GetComponentsInChildren<IConsentProvider>(true);
                foreach (IConsentProvider provider in found)
                    providers.Add(provider);
            }
        }

        public void Unload()
        {
            instance = null;
        }

        public static T GetProvider<T>() where T : class, IConsentProvider
        {
            if (instance == null) return null;

            for (int i = 0; i < instance.providers.Count; i++)
            {
                if (instance.providers[i] is T provider)
                    return provider;
            }

            return null;
        }
    }
}
