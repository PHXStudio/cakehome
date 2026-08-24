using System;
using UnityEngine;

namespace Watermelon
{
    [Serializable]
    public abstract class AdsProviderContainer
    {
        public abstract string ProviderName { get; }
        public abstract AdProviderHandler CreateHandler();
    }
}
