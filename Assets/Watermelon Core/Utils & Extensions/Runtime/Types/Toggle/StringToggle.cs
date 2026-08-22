namespace Watermelon
{
    [System.Serializable]
    public class StringToggle : ToggleType<string>
    {
        public StringToggle(bool enabled, string value) : base(enabled, value) { }
    }
}