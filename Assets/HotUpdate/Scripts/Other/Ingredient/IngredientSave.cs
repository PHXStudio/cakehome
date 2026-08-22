namespace Watermelon
{
    /// <summary>
    /// 原料存档：各原料增益的过期时间戳（二进制）。
    /// </summary>
    [System.Serializable]
    public class IngredientSave : ISaveObject
    {
        public long[] ActiveUntil = new long[0];

        public void EnsureInit(int count)
        {
            if (ActiveUntil == null || ActiveUntil.Length != count)
            {
                long[] next = new long[count];
                if (ActiveUntil != null)
                {
                    int copy = System.Math.Min(ActiveUntil.Length, count);
                    for (int i = 0; i < copy; i++)
                        next[i] = ActiveUntil[i];
                }

                ActiveUntil = next;
            }
        }

        public void OnBeforeSave()
        {
        }
    }
}
