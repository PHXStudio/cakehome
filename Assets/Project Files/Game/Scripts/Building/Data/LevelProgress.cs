using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class LevelProgress
    {
        [SerializeField] int   level;
        [SerializeField] float progress; // 0-1

        public int   Level    => level;
        public float Progress => progress;

        public LevelProgress() { }
        public LevelProgress(int level, float progress = 0f)
        {
            this.level = level;
            this.progress = progress;
        }
    }
}
