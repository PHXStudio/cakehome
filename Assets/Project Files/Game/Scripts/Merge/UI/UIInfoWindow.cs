using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIInfoWindow : UIPage
    {
        public override bool IsPopup => true;

        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button backgroundCloseButton;
        [SerializeField] Button closeButton;

        [Space]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text levelText;

        [Header("Grades")]
        [SerializeField] Transform gradesContainer;
        [SerializeField] UIInfoGradeCell gradeCellPrefab;

        [Header("Cross reference (Produced by / Produces)")]
        [SerializeField] GameObject crossRefSection;
        [SerializeField] TMP_Text crossRefTitleText;
        [SerializeField] Transform crossRefContainer;
        [SerializeField] UIInfoCrossRefCell crossRefCellPrefab;

        [Header("Layout Tuning")]
        [SerializeField] float sectionGap = 15f;
        [SerializeField] float crossRefBottomPadding = 33.8f;

        private const int ColumnsPerRow = 4;

        private UIFadeAnimation backFade;

        private RectTransform panelRT, gradesRT, crossRefSectionRT, crossRefContainerRT;
        private GridLayoutGroup gradesGrid, crossRefGrid;
        private float gradesTopOffset, crossRefBottomMargin, crossRefContainerTopOffset;

        public override void Init()
        {
            backFade = new UIFadeAnimation(gameObject);

            if (backgroundCloseButton != null) backgroundCloseButton.onClick.AddListener(OnCloseClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            backFade.Hide(immediately: true);
            panelScalable.Hide(immediately: true);

            panelRT = (RectTransform)panelScalable.Transform;
            gradesRT = (RectTransform)gradesContainer;
            crossRefSectionRT = (RectTransform)crossRefSection.transform;
            crossRefContainerRT = (RectTransform)crossRefContainer;
            gradesGrid = gradesContainer.GetComponent<GridLayoutGroup>();
            crossRefGrid = crossRefContainer.GetComponent<GridLayoutGroup>();

            gradesTopOffset = -gradesRT.anchoredPosition.y;
            crossRefBottomMargin = crossRefSectionRT.anchoredPosition.y - crossRefSectionRT.sizeDelta.y;
            crossRefContainerTopOffset = -crossRefContainerRT.anchoredPosition.y;
        }

        public static void Show(MergeFieldObject obj)
        {
            UIInfoWindow window = UIController.GetPage<UIInfoWindow>();
            if (window == null) return;

            window.SetData(obj);
            UIController.ShowPage(window);
        }

        // Opens the info panel for a spawner that isn't a live field instance — e.g. one
        // referenced from a cross-reference cell ("Produces" / "Produced by").
        public static void Show(MergeItemData itemData, int grade)
        {
            if (itemData == null) return;

            UIInfoWindow window = UIController.GetPage<UIInfoWindow>();
            if (window == null) return;

            window.titleText.text = itemData.GetGradeData(grade)?.DisplayName ?? itemData.TypeId;
            window.levelText.text = $"Lv.{grade}";

            int gradesCount = window.FillGrades(itemData.TypeId, itemData, grade);
            int crossRefCount = window.FillProduces(itemData, grade);
            window.ApplyLayout(gradesCount, crossRefCount);

            UIController.ShowPage(window);
        }

        private void SetData(MergeFieldObject obj)
        {
            titleText.text = obj.GetDisplayName();
            levelText.text = $"Lv.{obj.Grade}";

            int gradesCount = FillGrades(obj.TypeId, obj.ItemData, obj.Grade);

            int crossRefCount;
            if (obj is SpawnerObject)
                crossRefCount = FillProduces(obj.ItemData, obj.Grade);
            else if (obj is MergeItem || obj is CurrencyItem || obj is EnergyItem)
                crossRefCount = FillProducedBy(obj.TypeId);
            else
            {
                crossRefSection.SetActive(false);
                crossRefCount = 0;
            }

            ApplyLayout(gradesCount, crossRefCount);
        }

        private int FillGrades(string typeId, MergeItemData itemData, int currentGrade)
        {
            ClearChildren(gradesContainer);

            int maxGrade = itemData != null ? itemData.MaxGrade : 0;
            List<UIInfoGradeCell> cells = new List<UIInfoGradeCell>(maxGrade);

            for (int grade = 1; grade <= maxGrade; grade++)
            {
                UIInfoGradeCell cell = Instantiate(gradeCellPrefab, gradesContainer);
                cells.Add(cell);
                cell.SetLevel(grade);
                MergeGradeData gradeData = itemData.GetGradeData(grade);

                // Reaching currentGrade requires having merged through every grade below it,
                // so those are seen even if seenItemKeys (session-only, not saved) forgot them.
                bool seen = grade <= currentGrade || MergeController.Instance.IsGradeSeen(typeId, grade);
                bool hasInstance = MergeController.Instance.Grid.TryGetInstanceState(typeId, grade, out bool isLocked);

                // A live unlocked instance counts as open too — it may have reached the board
                // without going through a tracked merge (level config, spawner, etc.). Persist
                // the observation so it stays open even after this instance leaves the board.
                if (seen || (hasInstance && !isLocked))
                {
                    if (!seen)
                        MergeController.Instance.MarkGradeSeen(typeId, grade);

                    cell.SetOpen(gradeData?.Sprite, isSelected: grade == currentGrade);
                    continue;
                }

                if (hasInstance)
                    cell.SetFaded(gradeData?.Sprite);
                else
                    cell.SetMystery();
            }

            UpdateGradeArrows(cells);

            return maxGrade;
        }

        // Arrow between cells should only appear between two cells on the same row,
        // so it must be hidden for the last cell of each row and the very last cell.
        private void UpdateGradeArrows(List<UIInfoGradeCell> cells)
        {
            if (cells.Count == 0) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gradesContainer);

            for (int i = 0; i < cells.Count; i++)
            {
                bool isLastInRow = i == cells.Count - 1;
                if (!isLastInRow)
                {
                    float currentY = ((RectTransform)cells[i].transform).anchoredPosition.y;
                    float nextY = ((RectTransform)cells[i + 1].transform).anchoredPosition.y;
                    isLastInRow = !Mathf.Approximately(currentY, nextY);
                }

                cells[i].SetArrowVisible(!isLastInRow);
            }
        }

        private int FillProduces(MergeItemData itemData, int grade)
        {
            crossRefTitleText.text = "Produces";
            ClearChildren(crossRefContainer);

            var entries = itemData.GetGradeData(grade)?.GetConfig<SpawnPoolGradeConfig>()?.SpawnPool?.Items;
            if (entries == null)
            {
                crossRefSection.SetActive(false);
                return 0;
            }

            foreach (WeightedItem<SpawnEntry> entry in entries)
            {
                MergeItemData entryItemData = MergeDatabase.Instance.GetItem(entry.Item.typeId);
                MergeGradeData gradeData = entryItemData?.GetGradeData(entry.Item.grade);

                UIInfoCrossRefCell cell = Instantiate(crossRefCellPrefab, crossRefContainer);
                cell.SetIcon(gradeData?.Sprite);
                cell.SetSpawnerInfo(entryItemData != null && (entryItemData.ItemType == MergeItemType.Spawner || entryItemData.ItemType == MergeItemType.Chest) ? entryItemData : null, entry.Item.grade);
            }

            crossRefSection.SetActive(entries.Count > 0);

            return entries.Count;
        }

        private int FillProducedBy(string typeId)
        {
            ClearChildren(crossRefContainer);

            bool anyProducer = false;
            List<(MergeItemData candidate, int grade, bool isOpen)> known = new List<(MergeItemData, int, bool)>();

            foreach (MergeItemData candidate in MergeDatabase.Instance.Items)
            {
                if (candidate.ItemType != MergeItemType.Spawner && candidate.ItemType != MergeItemType.Chest) continue;

                int openGrade = -1;
                int lockedGrade = -1;

                for (int grade = 1; grade <= candidate.MaxGrade; grade++)
                {
                    WeightedList<SpawnEntry> pool = candidate.GetGradeData(grade)?.GetConfig<SpawnPoolGradeConfig>()?.SpawnPool;
                    if (pool == null) continue;

                    bool produces = false;
                    foreach (WeightedItem<SpawnEntry> entry in pool.Items)
                    {
                        if (entry.Item.typeId == typeId)
                        {
                            produces = true;
                            break;
                        }
                    }
                    if (!produces) continue;

                    anyProducer = true;

                    // Same "open" criteria as FillGrades: seen via a past merge, or a live
                    // unlocked instance currently on the board (which counts as discovered too).
                    bool seen = MergeController.Instance.IsGradeSeen(candidate.TypeId, grade);
                    bool hasInstance = MergeController.Instance.Grid.TryGetInstanceState(candidate.TypeId, grade, out bool isLocked);

                    if (seen || (hasInstance && !isLocked))
                    {
                        if (!seen)
                            MergeController.Instance.MarkGradeSeen(candidate.TypeId, grade);

                        openGrade = grade;
                    }
                    else if (hasInstance && isLocked)
                    {
                        lockedGrade = grade;
                    }
                }

                if (openGrade != -1)
                    known.Add((candidate, openGrade, true));
                else if (lockedGrade != -1)
                    known.Add((candidate, lockedGrade, false));
            }

            if (known.Count == 0)
            {
                crossRefTitleText.text = "Level up to produce";

                if (anyProducer)
                {
                    UIInfoCrossRefCell cell = Instantiate(crossRefCellPrefab, crossRefContainer);
                    cell.SetMystery();
                    cell.SetSpawnerInfo(null, 0);
                }

                crossRefSection.SetActive(anyProducer);
                return anyProducer ? 1 : 0;
            }

            crossRefTitleText.text = "Produced by";
            foreach ((MergeItemData candidate, int grade, bool isOpen) in known)
            {
                MergeGradeData gradeData = candidate.GetGradeData(grade);
                UIInfoCrossRefCell cell = Instantiate(crossRefCellPrefab, crossRefContainer);

                if (isOpen)
                    cell.SetIcon(gradeData?.Sprite);
                else
                    cell.SetFaded(gradeData?.Sprite);

                cell.SetSpawnerInfo(candidate, grade);
            }

            crossRefSection.SetActive(true);

            return known.Count;
        }

        private void ApplyLayout(int gradesCount, int crossRefCount)
        {
            int gradesRows = gradesCount > 0 ? Mathf.CeilToInt(gradesCount / (float)ColumnsPerRow) : 0;
            float gradesHeight = gradesRows * (gradesGrid.cellSize.y + gradesGrid.spacing.y);
            gradesRT.sizeDelta = new Vector2(gradesRT.sizeDelta.x, gradesHeight);

            float crossRefHeight = 0f;
            if (crossRefCount > 0)
            {
                int crossRefRows = Mathf.CeilToInt(crossRefCount / (float)ColumnsPerRow);
                crossRefHeight = crossRefContainerTopOffset + crossRefRows * (crossRefGrid.cellSize.y + crossRefGrid.spacing.y) + crossRefBottomPadding;

                crossRefSectionRT.sizeDelta = new Vector2(crossRefSectionRT.sizeDelta.x, crossRefHeight);
                crossRefSectionRT.anchoredPosition = new Vector2(crossRefSectionRT.anchoredPosition.x, crossRefHeight + crossRefBottomMargin);
            }

            float panelHeight = gradesTopOffset + gradesHeight + crossRefBottomMargin;
            if (crossRefCount > 0) panelHeight += sectionGap + crossRefHeight;

            panelRT.sizeDelta = new Vector2(panelRT.sizeDelta.x, panelHeight);
        }

        // Deactivate before Destroy: Destroy() defers removal until end of frame, but the
        // layout rebuild right after this (ForceRebuildLayoutImmediate) runs in the same
        // frame and would otherwise still count these stale children, shifting row positions.
        private static void ClearChildren(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                GameObject child = container.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        protected override void OnShow()
        {
            backFade.Show(0.2f, onCompleted: () =>
            {
                panelScalable.Show(immediately: false, duration: 0.3f);
            });

            NotifyOpened();
        }

        protected override void OnHide()
        {
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.4f, onCompleted: () =>
            {
                NotifyClosed();
            });
        }

        private void OnCloseClicked()
        {
            UIController.HidePage<UIInfoWindow>();
        }
    }
}
