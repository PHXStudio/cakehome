using System;

namespace Watermelon
{
    [Serializable]
    public class ResourcesSave : ISaveObject
    {
        public int Energy;
        public long EnergyTimestampBinary;
        public bool IsEnergyFirstTimeFree = true;

        public void OnBeforeSave() { }
    }
}
