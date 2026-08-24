using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Abstract base class for all reward view types.
    /// A view is responsible for displaying a single <see cref="Reward"/> inside a <see cref="RewardsHolder"/>.
    /// Each concrete <see cref="Reward"/> subclass should have a matching concrete RewardView subclass,
    /// linked via <see cref="RegisterRewardAttribute"/> for automatic code-gen mapping.
    /// </summary>
    [Serializable]
    public abstract class RewardView
    {
        protected Reward reward;
        public Reward Reward => reward;

        protected IRewardHolder holder;

        internal void Init(Reward reward, IRewardHolder holder)
        {
            this.reward = reward;
            this.holder = holder;

            OnInitialized();
        }

        protected abstract void OnInitialized();

        public virtual void OnPurchased() { }
        public virtual void OnDestroy() { }

        /// <summary>
        /// Populates view fields from the given reward data. Called by the editor when auto-populating views.
        /// Override in subclasses to pre-fill serialized fields (e.g. icon, text) based on reward state.
        /// </summary>
        public virtual void Populate(Reward reward)
        {
            LogManager.LogWarning($"[{GetType().Name}] Populate() is not overridden. View fields will not be pre-filled by the editor.", LogCategory.Systems);
        }

        public void MarkAsDirty()
        {
            holder?.MarkAsDirty();
        }
    }
}
