namespace Watermelon
{
    // Must be a class (not struct) — WeightedList<T> requires T : class
    [System.Serializable]
    public class SpawnEntry
    {
        public string typeId;
        public int    grade;
    }
}
