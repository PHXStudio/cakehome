namespace Watermelon
{
    [System.Serializable]
    public class AudioSave : ISaveObject
    {
        public VolumeData[] VolumeDatas;

        public void OnBeforeSave() { }

        [System.Serializable]
        public class VolumeData
        {
            public AudioType AudioType;
            public float Volume;
        }
    }
}