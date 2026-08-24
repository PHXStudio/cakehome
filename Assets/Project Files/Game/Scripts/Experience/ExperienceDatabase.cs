using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Experience Database", menuName = "Game/Experience Database")]
    public class ExperienceDatabase : ScriptableObject
    {
        // levels[0] is the reward for reaching level 2 (level 1 is the implicit start, has no
        // reward) — array index i corresponds to level (i + 2). Level/progress themselves are
        // designer-placed via ZoneProgressionStep.LevelProgress, not derived from any threshold.
        [SerializeField] LevelThreshold[] levels;
        [SerializeField] Sprite expIcon;

        [SerializeField] AudioClip appearAudioClip;
        [SerializeField] AudioClip collectAudioClip;

        public int    MaxLevel => (levels?.Length ?? 0) + 1;
        public Sprite ExpIcon  => expIcon;

        public AudioClip AppearAudioClip  => appearAudioClip;
        public AudioClip CollectAudioClip => collectAudioClip;

        public RewardBundle GetLevelReward(int level)
        {
            if (levels == null || level <= 1) return null;
            int index = Mathf.Clamp(level - 2, 0, levels.Length - 1);
            return levels[index].reward;
        }
    }

    [System.Serializable]
    [CustomArrayElement]
    public class LevelThreshold
    {
        public RewardBundle reward;

        // Picked up by ArrayElementGUIRenderer via reflection to title inspector array elements.
        private string GetCustomArrayTitle(int index) => $"Level {index + 2}";
    }
}
