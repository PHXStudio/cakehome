using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIClientOrderCard : MonoBehaviour, ITaskCardView
    {
        [SerializeField] Image characterPortrait;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] RectTransform rewardIconRect;
        [SerializeField] Transform itemsContainer;
        [SerializeField] UIOrderItemIcon itemIconPrefab;
        [SerializeField] Button giveButton;
        [SerializeField] LayoutElement layoutElement;

        private const float MinPreferredWidth = 340f;
        private const float PreferredWidthBase = 60f;
        private const float PreferredWidthPerItem = 170f;

        private ClientOrderTask task;
        private readonly List<UIOrderItemIcon> spawnedIcons = new List<UIOrderItemIcon>();
        private bool giving;
        private bool giveButtonWasActive;

        public Button GiveButton => giveButton;

        public void Setup(ClientOrderTask task)
        {
            this.task = task;

            if (task.Character != null)
                characterPortrait.sprite = task.Character.GetPortrait(EmotionType.Default);

            rewardText.text = $"+{task.CoinsReward}";

            foreach (OrderItem item in task.Items)
            {
                UIOrderItemIcon iconView = Instantiate(itemIconPrefab, itemsContainer);
                spawnedIcons.Add(iconView);
            }

            task.OnProgressChanged += Refresh;
            task.OnItemBound += OnItemBound;
            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (task == null) return;
            task.OnProgressChanged -= Refresh;
            task.OnItemBound -= OnItemBound;
        }

        private void OnItemBound(MergeFieldObject obj)
        {
            obj.SetTaskBounce(true);
        }

        private void Refresh()
        {
            if (giving) return; // ignore transient reverts caused by our own Give consumption

            for (int i = 0; i < task.Items.Count; i++)
            {
                OrderItem item = task.Items[i];
                Sprite sprite = MergeDatabase.Instance?.GetItem(item.typeId)?.GetGradeData(item.grade)?.Sprite;
                spawnedIcons[i].SetState(sprite, item.collected);
            }

            if (task.IsComplete && !giveButtonWasActive)
                AudioController.PlaySound(AudioController.GetClip("task_complete"));

            giveButtonWasActive = task.IsComplete;
            giveButton.gameObject.SetActive(task.IsComplete);
        }

        public void OnGiveClicked()
        {
            if (giving || !task.IsComplete) return;
            giving = true;

            Checkpoint.Log($"Client order fulfilled: {task.TaskId}, reward {task.CoinsReward} coins", gameObject);

            giveButton.gameObject.SetActive(false);
            Unsubscribe(); // stop reacting to the field-changed churn our own removals will cause

            foreach (OrderItem item in task.Items)
            {
                if (item.boundObject == null) continue;
                item.boundObject.SetTaskBounce(false);
                MergeController.Instance.RemoveFromGrid(item.boundObject);
            }

            // Credit on first cloud element hit so the panel text updates when the cloud lands
            int coinsReward = task.CoinsReward;
            RectTransform target = UIController.GetPage<UIHeader>()?.CoinsPanel?.TextRectTransform;
            if (target != null)
            {
                RectTransform source = rewardIconRect != null ? rewardIconRect : (RectTransform)transform;
                CurrencyCloud.SpawnCurrency("Coins", source, target, Mathf.Min(coinsReward, 12), $"+{coinsReward}", () => CurrencyController.Add(CurrencyType.Coins, coinsReward, "client_order"));
            }
            else
            {
                CurrencyController.Add(CurrencyType.Coins, coinsReward, "client_order");
            }

            // Merge stats + bridge notification (daily tasks etc.)
            MergeStatsController.AddOrdersCompleted(1);
            // 碎片功能已禁用（2026-08-24）：恢复时取消下行注释 + 重新激活 Fragment Button + FragmentDropHook
            // FragmentController.Add(task.Items.Count >= 3 ? 2 : 1);
            CakeUIBridge.OrderCompleted?.Invoke(1);

            TaskController.Instance.RemoveTask(task);
        }

        public void PlayEnter() => TaskCardAnimation.PlayEnter(transform, layoutElement, CalculatePreferredWidth(task.Items.Count));
        public void PlayExit(Action onComplete) => TaskCardAnimation.PlayExit(transform, layoutElement, onComplete);

        // Width grows with item count so wider orders get more breathing room; clamped to a
        // sane minimum for 0-1 item orders. Fitted to designer-provided points: 2 -> 400, 3 -> 570, 4 -> 740.
        private static float CalculatePreferredWidth(int itemCount) => Mathf.Max(MinPreferredWidth, PreferredWidthBase + PreferredWidthPerItem * itemCount);
    }
}
