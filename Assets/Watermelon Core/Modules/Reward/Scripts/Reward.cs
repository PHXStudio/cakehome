using System;
using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Abstract base class for all reward types.
    /// Subclass this to implement a concrete reward (e.g. currency grant, skin unlock).
    /// Decorate concrete subclasses with <see cref="RegisterRewardAttribute"/> so the
    /// code generator can pair them with their corresponding <see cref="RewardView"/>.
    /// </summary>
    [Serializable]
    public abstract class Reward
    {
        public abstract void ApplyReward();
        public virtual void RestoreReward() { }

        public virtual List<IRewardPreview> GetRewardPreviews()
        {
            return null;
        }

        /// <summary>
        /// Return true if you want to disable offer object.
        /// For example: if you already purchased NoAds as a part of the Pack, you want other NoAds offers to be disabled.
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckDisableState() { return false; }
    }
}
