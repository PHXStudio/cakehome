namespace Watermelon
{
    [System.Serializable]
    public class DoubleToggle : ToggleType<double>
    {
        public DoubleToggle(bool enabled, double value) : base(enabled, value) { }
    }
}