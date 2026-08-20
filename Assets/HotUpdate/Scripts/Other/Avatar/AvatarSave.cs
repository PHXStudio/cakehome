namespace Watermelon
{
    /// <summary>
    /// Avatar 存档：已购换装槽位 + 当前装备 + 称号解锁位掩码。
    /// </summary>
    [System.Serializable]
    public class AvatarSave : ISaveObject
    {
        public bool[] OwnedSlots = new bool[0];
        public int EquippedSlot = -1;
        public int UnlockedTitles = 0;

        public void EnsureInit(int slotCount)
        {
            if (OwnedSlots == null || OwnedSlots.Length != slotCount)
            {
                bool[] next = new bool[slotCount];
                if (OwnedSlots != null)
                {
                    int copy = System.Math.Min(OwnedSlots.Length, slotCount);
                    for (int i = 0; i < copy; i++)
                        next[i] = OwnedSlots[i];
                }

                OwnedSlots = next;
            }
        }

        public void Flush()
        {
        }
    }
}
