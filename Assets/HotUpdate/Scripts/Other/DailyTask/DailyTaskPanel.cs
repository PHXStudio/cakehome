using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 每日任务面板：4 个任务（进度 x/y + 领取）。由 DailyTaskUIBuilder 构建。
    /// 进度经 DailyTaskController.OnTaskProgressChanged 事件驱动刷新。
    /// </summary>
    public class DailyTaskPanel : MonoBehaviour, IPopupWindow
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button closeButton;
        [SerializeField] RectTransform taskContainer;
        [SerializeField] GameObject rowTemplate;

        private UIFadeAnimation backFade;
        private readonly List<TaskRow> rows = new List<TaskRow>();
        private bool isInitialized;

        public bool IsOpened => gameObject.activeSelf;

        private sealed class TaskRow
        {
            public RectTransform Root;
            public TMP_Text NameText;
            public TMP_Text ProgressText;
            public Button ClaimButton;
            public TMP_Text ClaimLabel;
            public DailyTaskType Type;
        }

        public void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            backFade = new UIFadeAnimation(gameObject);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
            DailyTaskController.OnTaskProgressChanged += Refresh;

            backFade.Hide(immediately: true);
            panelScalable.Hide(immediately: true);
        }

        public void Show()
        {
            if (IsOpened)
                return;

            gameObject.SetActive(true);
            Refresh();

            backFade.Show(0.2f, onCompleted: () =>
            {
                panelScalable.Show(immediately: false, duration: 0.3f);
            });

        }

        public void Hide()
        {
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.3f, onCompleted: () =>
            {
                gameObject.SetActive(false);
            });

        }

        private void Refresh()
        {
            if (taskContainer == null)
                return;

            for (int i = 0; i < 4; i++)
            {
                TaskRow row = GetOrCreateRow(i);
                DailyTaskType type = (DailyTaskType)i;
                row.Type = type;

                row.NameText.text = DailyTaskController.GetTaskName(type);

                int progress = DailyTaskController.GetProgress(type);
                int target = DailyTaskController.GetTaskTarget(type);
                bool claimed = DailyTaskController.IsClaimed(type);

                row.ProgressText.text = claimed ? "已领取" : $"{progress}/{target}";
                row.ClaimButton.interactable = DailyTaskController.CanClaim(type);
                row.ClaimLabel.text = claimed ? "已领" : "领取";
            }
        }

        private TaskRow GetOrCreateRow(int index)
        {
            if (index < rows.Count)
                return rows[index];

            GameObject rowGo = Instantiate(rowTemplate, taskContainer);
            rowGo.SetActive(true);

            TaskRow row = new TaskRow
            {
                Root = rowGo.GetComponent<RectTransform>(),
                NameText = rowGo.transform.Find("Name")?.GetComponent<TMP_Text>(),
                ProgressText = rowGo.transform.Find("Progress")?.GetComponent<TMP_Text>(),
                ClaimButton = rowGo.transform.Find("Claim")?.GetComponent<Button>(),
                ClaimLabel = rowGo.transform.Find("Claim/Text")?.GetComponent<TMP_Text>()
            };

            row.ClaimButton.onClick.RemoveAllListeners();
            row.ClaimButton.onClick.AddListener(() => OnClaimClicked(row));

            rows.Add(row);
            return row;
        }

        private void OnClaimClicked(TaskRow row)
        {
            if (DailyTaskController.Claim(row.Type))
                Refresh();
        }
    }
}
