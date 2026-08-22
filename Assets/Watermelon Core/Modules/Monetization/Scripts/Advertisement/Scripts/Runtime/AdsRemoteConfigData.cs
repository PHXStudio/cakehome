#if MODULE_REMOTE_CONFIG
namespace Watermelon
{
    [System.Serializable]
    public class AdsRemoteConfigData : RemoteConfigData
    {
        public override string Key => "ads";

        public int intDelay = 30;
        public int intSDelay = 40;
        public int intFSDelay = 40;

        public bool useBanner = true;
        public bool useInterstitials = true;
        public bool useRewardedVideo = true;
    }
}
#endif
