using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    // Horizontal scroll view that hosts task cards.
    // Diffs against TaskController.ActiveTasks on every OnTasksChanged — only the cards whose
    // task was actually added/removed are instantiated/destroyed, so unrelated cards are never
    // disturbed (and can run their own un-interrupted enter animation).
    public class UITaskPanel : MonoBehaviour
    {
        [SerializeField] Transform cardsContainer;
        [SerializeField] UIClientOrderCard clientOrderCardPrefab;
        [SerializeField] UISpawnerRewardCard spawnerRewardCardPrefab;
        [SerializeField] UIHammerCard       hammerCardPrefab;
        [SerializeField] UIPlaceholderCard  placeholderCardPrefab;

        public static event Action<ITask, UIClientOrderCard> OnClientOrderCardCreated;

        private static UITaskPanel instance;

        private readonly Dictionary<ITask, MonoBehaviour> activeCards = new Dictionary<ITask, MonoBehaviour>();
        private UISpawnerRewardCard activeSpawnerCard;
        private UIHammerCard activeHammerCard;

        // Lets a tutorial step that starts after a card was already created (e.g. resuming from a
        // relaunch) find it directly, rather than only relying on OnClientOrderCardCreated timing.
        public static UIClientOrderCard FindOrderCard(string taskId)
        {
            if (instance == null) return null;

            foreach (var pair in instance.activeCards)
            {
                if (pair.Key is ClientOrderTask order && order.TaskId == taskId && pair.Value is UIClientOrderCard card)
                    return card;
            }

            return null;
        }

        // Lets a tutorial highlight the pinned spawner-reward card without needing its own event.
        public static UISpawnerRewardCard ActiveSpawnerCard => instance?.activeSpawnerCard;

        // Lets a tutorial highlight the pinned hammer/upgrade-hint card without needing its own event.
        public static UIHammerCard ActiveHammerCard => instance?.activeHammerCard;

        private void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            TaskController.OnTasksChanged += Refresh;
            CurrencyController.SubscribeGlobalCallback(OnCurrencyChanged);
            BuildingController.OnUpgradeCompleted += OnBuildingUpgraded;

            if (TaskController.Instance != null)
            {
                TaskController.Instance.SpawnerQueue.OnChanged += RefreshSpawnerCard;
                Refresh();
            }

            RefreshHammerCard();
        }

        private void OnDisable()
        {
            TaskController.OnTasksChanged -= Refresh;
            CurrencyController.UnsubscribeGlobalCallback(OnCurrencyChanged);
            BuildingController.OnUpgradeCompleted -= OnBuildingUpgraded;

            if (TaskController.Instance != null)
                TaskController.Instance.SpawnerQueue.OnChanged -= RefreshSpawnerCard;
        }

        private void Refresh()
        {
            IReadOnlyList<ITask> tasks = TaskController.Instance.ActiveTasks;
            var current = new HashSet<ITask>(tasks);

            // Remove cards for tasks no longer active.
            var toRemove = new List<ITask>();
            foreach (var pair in activeCards)
            {
                if (current.Contains(pair.Key)) continue;
                toRemove.Add(pair.Key);

                MonoBehaviour card = pair.Value;
                if (card is ITaskCardView animated)
                    animated.PlayExit(() => { if (card != null) Destroy(card.gameObject); });
                else
                    Destroy(card.gameObject);
            }
            foreach (ITask task in toRemove)
                activeCards.Remove(task);

            // Add cards for newly active tasks.
            foreach (ITask task in tasks)
            {
                if (activeCards.ContainsKey(task)) continue;

                MonoBehaviour card = null;
                if (task is ClientOrderTask order)
                {
                    var view = Instantiate(clientOrderCardPrefab, cardsContainer);
                    view.Setup(order);
                    card = view;
                    OnClientOrderCardCreated?.Invoke(order, view);
                }
                else if (task is PlaceholderTask placeholder)
                {
                    var view = Instantiate(placeholderCardPrefab, cardsContainer);
                    view.Setup(placeholder);
                    card = view;
                }

                if (card == null) continue;

                activeCards[task] = card;
                if (card is ITaskCardView animated)
                    animated.PlayEnter();
            }

            RefreshSpawnerCard();
        }

        // One card represents the whole LIFO queue: it always shows the queue's top entry,
        // stays pinned as the first card, and only leaves once the queue is drained.
        private void RefreshSpawnerCard()
        {
            SpawnerRewardQueue queue = TaskController.Instance.SpawnerQueue;

            if (queue.HasPending)
            {
                if (activeSpawnerCard == null)
                {
                    activeSpawnerCard = Instantiate(spawnerRewardCardPrefab, cardsContainer);
                    activeSpawnerCard.transform.SetAsFirstSibling();
                    activeSpawnerCard.PlayEnter();
                }

                activeSpawnerCard.Setup(queue.Peek(), OnSpawnerCardClaimed);
            }
            else if (activeSpawnerCard != null)
            {
                UISpawnerRewardCard card = activeSpawnerCard;
                activeSpawnerCard = null;
                card.PlayExit(() => { if (card != null) Destroy(card.gameObject); });
            }
        }

        private void OnSpawnerCardClaimed()
        {
            TaskController.Instance.SpawnerQueue.Pop();
        }

        private void OnCurrencyChanged(Currency currency, int difference) => RefreshHammerCard();

        private void OnBuildingUpgraded(ZoneData zone, string buildingId, int totalUpgradesInZone) => RefreshHammerCard();

        private void RefreshHammerCard()
        {
            bool canUpgrade = BuildingController.CanUpgradeAnyBuilding(BuildingController.CurrentZone);

            if (canUpgrade)
                ShowHammerCard();
            else if (activeHammerCard != null)
            {
                UIHammerCard card = activeHammerCard;
                activeHammerCard = null;
                card.PlayExit(() => { if (card != null) Destroy(card.gameObject); });
            }
        }

        public void ShowHammerCard()
        {
            if (activeHammerCard != null) return;

            AudioController.PlaySound(AudioController.GetClip("building_available"));

            activeHammerCard = Instantiate(hammerCardPrefab, cardsContainer);
            // Pinned right after the spawner-reward card (which stays absolute-first) — see
            // RefreshSpawnerCard's SetAsFirstSibling for the sibling this is pinned relative to.
            activeHammerCard.transform.SetSiblingIndex(activeSpawnerCard != null ? 1 : 0);
            activeHammerCard.Setup(() =>
            {
                MergeViewController.SetBuildingActive(true);
                UIBuilding.Show();
            });
            activeHammerCard.PlayEnter();
        }
    }
}
