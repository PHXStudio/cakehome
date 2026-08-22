using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Watermelon
{
    /// <summary>
    /// Abstract MonoBehaviour that owns a <see cref="RewardsSet"/> and a list of <see cref="RewardView"/> instances.
    /// Subclass this to create concrete UI reward holders (e.g. shop buttons, daily-reward panels).
    /// On initialization it maps each reward in the set to its view via <see cref="RewardsMap"/>,
    /// then delegates popup display to <see cref="RewardsPopup"/> when the reward is applied.
    /// Implements <see cref="IUIPageElement"/> so the containing page can notify it of visibility changes.
    /// </summary>
    public abstract class RewardsHolder : MonoBehaviour, IUIPageElement, IRewardHolder
    {
        [Group("System")]
        [SerializeField, CreateScriptableObject]
        protected RewardsSet rewardSet;

        // Serialized list required for [SerializeReference] polymorphism.
        // At runtime a Dictionary is built in InitializeComponents() for O(1) lookup.
        [SerializeReference] List<RewardView> rewardsView = new List<RewardView>();

        [Group("Events")]
        [SerializeField] UnityEvent rewardReceived;
        public UnityEvent RewardReceived => rewardReceived;

        [Group("Settings")]
        [SerializeField, Space]
        private bool showPopup = true;

        protected bool isPageActive;
        protected bool isInitialized;

        protected bool isDirty;
        public bool IsDirty => isDirty;

        // Built from rewardsView at initialization for O(1) type-based lookup.
        private Dictionary<Type, RewardView> viewsLookup;

        protected void InitializeComponents()
        {
            if (isInitialized) return;

            if(rewardSet == null)
            {
                Debug.LogError("Rewards Set isn't linked!");
                return;
            }

            isInitialized = true;

            // Build runtime lookup dictionary from the serialized list.
            viewsLookup = new Dictionary<Type, RewardView>(rewardsView.Count);
            foreach (RewardView view in rewardsView)
            {
                if (view != null)
                    viewsLookup[view.GetType()] = view;
            }

            List<Reward> rewards = rewardSet.Rewards;
            if(!rewards.IsNullOrEmpty())
            {
                foreach (Reward reward in rewards)
                {
                    Type viewType = RewardsMap.GetView(reward.GetType());
                    if (viewType == null) continue;

                    RewardView view = GetView(viewType);
                    if (view == null) continue;

                    view.Init(reward, this);
                }
            }

            if (CheckDisableState())
                gameObject.SetActive(false);

            rewardSet.RewardReceived += OnRewardReceived;
        }

        public void Init(UIPage page)
        {
            isPageActive = page.IsPageDisplayed;
        }

        private void Update()
        {
            if (!isInitialized) return;
            if (!isPageActive) return;
            if (!isDirty) return;

            List<Reward> rewards = rewardSet.Rewards;
            if (!rewards.IsNullOrEmpty())
            {
                foreach (Reward reward in rewards)
                {
                    if (reward == null) continue;
                    if (reward.CheckDisableState())
                    {
                        gameObject.SetActive(false);
                        break;
                    }
                }
            }

            isDirty = false;
        }

        private void OnDestroy()
        {
            if (!rewardsView.IsNullOrEmpty())
            {
                for (int i = 0; i < rewardsView.Count; i++)
                {
                    rewardsView[i]?.OnDestroy();
                }
            }

            if(rewardSet != null)
                rewardSet.RewardReceived -= OnRewardReceived;
        }

        public bool CheckDisableState()
        {
            if (!isInitialized) return false;

            List<Reward> rewards = rewardSet.Rewards;
            if (!rewards.IsNullOrEmpty())
            {
                foreach (Reward reward in rewards)
                {
                    if (reward == null) continue;
                    if (reward.CheckDisableState())
                        return true;
                }
            }

            return false;
        }

        private void OnRewardReceived()
        {
            if (!isInitialized) return;

            void InvokeRewardCallback()
            {
                rewardReceived?.Invoke();

                if (!rewardsView.IsNullOrEmpty())
                {
                    for (int i = 0; i < rewardsView.Count; i++)
                        rewardsView[i].OnPurchased();
                }
            }

            if (showPopup)
            {
                if (!RewardsPopup.Display(rewardSet.GetPreviews(), () => InvokeRewardCallback()))
                    InvokeRewardCallback();
            }
            else
            {
                InvokeRewardCallback();
            }
        }

        public void OnPageStateChanged(bool state)
        {
            isPageActive = state;

            if (!isInitialized) return;

            if (state)
            {
                List<Reward> rewards = rewardSet.Rewards;
                if (!rewards.IsNullOrEmpty())
                {
                    foreach (Reward reward in rewards)
                    {
                        if (reward == null) continue;
                        if (reward.CheckDisableState())
                        {
                            gameObject.SetActive(false);

                            break;
                        }
                    }

                }

                isDirty = false;
            }
        }

        public void MarkAsDirty()
        {
            isDirty = true;
        }

        public RewardView GetView(Type type)
        {
            if (viewsLookup == null) return null;
            viewsLookup.TryGetValue(type, out RewardView view);
            return view;
        }
    }
}
