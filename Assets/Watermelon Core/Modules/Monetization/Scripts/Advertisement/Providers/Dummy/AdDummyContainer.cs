#pragma warning disable 0649

using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class AdDummyContainer : AdsProviderContainer
    {
        [SerializeField] BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;

        public override string ProviderName => "Dummy";
        public override AdProviderHandler CreateHandler() => new AdDummyHandler();
    }
}