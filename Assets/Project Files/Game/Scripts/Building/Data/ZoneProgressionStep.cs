using UnityEngine;
using UnityEngine.Serialization;

namespace Watermelon
{
    [System.Serializable]
    public class ZoneProgressionStep
    {
        [Tooltip("Everything bellow will be applied after specified level")]
        [SerializeField] int           atUpgrade;
        [Tooltip("Specified exp level will be applied right after this step starts")]
        [SerializeField] LevelProgress targetLevel;
        [Tooltip("This dialog will be played right after this step starts")]
        [SerializeField] DialogData    dialog;
        [Tooltip("This reward will be received right after this step starts")]
        [SerializeField] RewardBundle  reward;

        [Space]
        [Tooltip("This tasks will be spawned after this progression step starts")]
        [SerializeField] ProgressionTaskEntry[]    sequentialTasks;
        [SerializeField] RandomProgressionTaskPool randomTasks;

        public int           AtUpgrade      => atUpgrade;
        public LevelProgress TargetLevel    => targetLevel;
        public bool          HasTargetLevel => targetLevel != null && targetLevel.Level > 0;
        public DialogData    Dialog         => dialog;
        public RewardBundle  Reward         => reward;

        public ProgressionTaskEntry[]    SequentialTasks => sequentialTasks;
        public RandomProgressionTaskPool RandomTasks     => randomTasks;
    }
}
