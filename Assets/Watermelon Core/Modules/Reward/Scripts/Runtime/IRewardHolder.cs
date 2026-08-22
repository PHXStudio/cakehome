namespace Watermelon
{
    /// <summary>
    /// Implemented by objects that own one or more <see cref="RewardView"/> instances
    /// and support a deferred dirty-state refresh cycle.
    /// <see cref="RewardView.MarkAsDirty"/> calls through to <see cref="MarkAsDirty"/>
    /// so the holder can schedule a UI refresh on the next frame.
    /// </summary>
    public interface IRewardHolder
    {
        bool IsDirty { get; }
        void MarkAsDirty();
    }
}
