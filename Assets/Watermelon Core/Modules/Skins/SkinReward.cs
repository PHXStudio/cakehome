using UnityEngine;

namespace Watermelon
{
    public class SkinReward : Reward
    {
        [SkinPicker]
        [SerializeField] string skinID;

        [SerializeField] bool disableIfSkinIsUnlocked;

        public override void ApplyReward()
        {
            SkinController.Instance.UnlockSkin(skinID, true);
        }

        public override bool CheckDisableState()
        {
            if (disableIfSkinIsUnlocked)
            {
                return SkinController.Instance.IsSkinUnlocked(skinID);
            }

            return false;
        }
    }
}
