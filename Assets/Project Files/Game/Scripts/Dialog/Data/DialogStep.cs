namespace Watermelon
{
    [System.Serializable]
    public abstract class DialogStep
    {
        public abstract bool IsSkippable { get; }
    }
}
