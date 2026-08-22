using System;

namespace Watermelon
{
    [Serializable]
    public class ExperienceSave : ISaveObject
    {
        public int   CurrentLevel = 1;
        public float CurrentLevelProgress;

        public void OnBeforeSave() { }
    }
}
