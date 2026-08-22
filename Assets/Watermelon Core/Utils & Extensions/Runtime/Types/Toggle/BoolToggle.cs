namespace Watermelon
{
    [System.Serializable]
    public class BoolToggle : ToggleType<bool>
    {
        public BoolToggle(bool enabled, bool value) : base(enabled, value) { }
    }
}