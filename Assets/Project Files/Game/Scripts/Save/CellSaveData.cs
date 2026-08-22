using System;

namespace Watermelon
{
    [Serializable]
    public class CellSaveData
    {
        public int X;
        public int Y;
        public string TypeId;
        public int Grade;
        public bool IsHalfLocked;
        public bool IsLocked;
        public string CustomData;
    }
}
