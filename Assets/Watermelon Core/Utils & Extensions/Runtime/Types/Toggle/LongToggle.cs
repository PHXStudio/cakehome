namespace Watermelon
{
    [System.Serializable]
    public class LongToggle : ToggleType<long>
    {
        public LongToggle(bool enabled, long value) : base(enabled, value) { }
    }
}