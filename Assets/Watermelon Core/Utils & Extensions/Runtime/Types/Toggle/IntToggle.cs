namespace Watermelon
{
    [System.Serializable]
    public class IntToggle : ToggleType<int>
    {
        public IntToggle(bool enabled, int value) : base(enabled, value) { }
    }
}