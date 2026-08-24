namespace Watermelon
{
    /// <summary>
    /// Marker interface for analytics event payloads passed to
    /// <see cref="Analytics.TrackEvent"/>.
    /// Implement this on plain data classes (e.g. <c>AnalyticsIAPData</c>) to attach
    /// structured parameters to an event without coupling the core analytics system
    /// to module-specific types.
    /// </summary>
    public interface IAnalyticsData { }
}
