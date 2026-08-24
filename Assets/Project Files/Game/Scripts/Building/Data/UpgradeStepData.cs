using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class UpgradeStepData
    {
        [SerializeField] int    cost;
        [SerializeField] Sprite upgradedSprite;
        [SerializeField] BuildingCompletionThought completionThought;

        public int    Cost           => cost;
        public Sprite UpgradedSprite => upgradedSprite;

        public bool                     HasCompletionThought => completionThought != null && !string.IsNullOrEmpty(completionThought.Text);
        public BuildingCompletionThought CompletionThought    => completionThought;
    }
}
