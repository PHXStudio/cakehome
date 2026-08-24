namespace Watermelon
{
    public interface ISDKTaskBehavior
    {
        public LoadingTask Task { get; }

        public void Init(SDKInitializer initializer);
    }
}
