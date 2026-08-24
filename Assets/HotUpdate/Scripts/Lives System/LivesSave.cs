using System;

namespace Watermelon
{
    [System.Serializable]
    public class LivesSave : ISaveObject
    {
        public int LivesCount = -1;

        public bool LifeLocked = false;
        public long NewLifeDateBinary;

        public bool InfiniteLives = false;
        public long InfiniteLivesDateBinary;

        [NonSerialized] LivesStatus status;

        public void Init(LivesStatus status)
        {
            this.status = status;
        }

        public void OnBeforeSave()
        {
            if (status == null) return;

            InfiniteLives = status.InfiniteMode;
            InfiniteLivesDateBinary = status.InfiniteModeDate.ToBinary();

            // LivesCount / NewLifeDateBinary 为旧命数制的废弃字段，不再回写（能量由 ResourcesSave 承载）
        }
    }
}