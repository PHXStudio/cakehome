namespace Watermelon
{
    [System.Serializable]
    public class FloatToggle : ToggleType<float>
    {
        public FloatToggle(bool enabled, float value) : base(enabled, value) { }
    }
}