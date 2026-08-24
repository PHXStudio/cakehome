using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon
{
    public class MergeController : MonoBehaviour
    {
        [SerializeField] MergeGrid mergeGrid;
        [SerializeField] MergeDatabase database;

        public MergeGrid Grid => mergeGrid;

        [Header("Drag")]
        [SerializeField] float dragYOffset = 40f;
        [SerializeField] float dragScale = 1.2f;
        [SerializeField] float returnFlightDuration = 0.2f;
        [SerializeField] float displaceFlightDuration = 0.2f;
        [SerializeField] Ease.Type flightEase = Ease.Type.CubicOut;

        [Header("Merge Hint")]
        [SerializeField] float mergeHintDelay = 7f;
        [SerializeField] float hintBounceScale = 1.15f;

        [Header("Delete")]
        [SerializeField] float deleteScaleDuration = 0.25f;
        [SerializeField] Ease.Type deleteScaleEase = Ease.Type.CubicIn;

        [Header("Spawn Feedback")]
        [SerializeField] Color greatItemTextColor = new Color(1f, 0.84f, 0f);
        public Color GreatItemTextColor => greatItemTextColor;

        public static MergeController Instance { get; private set; }

        public static event Action<MergeFieldObject> OnObjectSelected;
        public static event Action OnObjectDeselected;
        public static event Action<Sprite> OnItemDeleted;
        public static event Action<MergeFieldObject> OnMerged;

        // Tutorial-only gate — while locked, no item can be picked up/dragged, so a scripted step
        // (e.g. submitting the guided order) can't have its target item merged away or displaced.
        private static bool isDragLocked;

        public static void SetDragLocked(bool locked)
        {
            isDragLocked = locked;
        }

        // Selection
        private MergeFieldObject selectedObject;

        // Drag
        private bool isDragging;
        private MergeFieldObject draggedObject;
        private Vector2Int dragSourcePos;
        private MergeFlyingIcon dragIcon;
        private Canvas canvas;
        private Vector2 dragStartScreenPos;
        private Vector2 dragStartIconPos;

        // Magnet (hover preview while dragging near a matching item)
        private MergeFieldObject magnetTarget;     // nearest in-radius candidate — used for merge resolution on drop
        private Vector2Int magnetTargetPos;
        private float magnetTargetDist;
        private readonly List<MergeCell> magnetCandidates = new List<MergeCell>();
        private readonly HashSet<MergeCell> magnetActiveCells = new HashSet<MergeCell>(); // all candidates currently pulled in

        // Merge hint
        private float timeSinceLastMerge;
        private bool isShowingHint;
        private Coroutine hintCoroutineA;
        private Coroutine hintCoroutineB;
        private MergeFieldObject hintObjA;
        private MergeFieldObject hintObjB;

        // New-item tracking (persists to ProgressSave in Phase 3+)
        private readonly HashSet<string> seenItemKeys = new HashSet<string>();

        // Undo (Delete)
        private (string TypeId, int Grade, int X, int Y)? pendingDelete;

        // Only one "New Item!" toast may be visible at a time, regardless of source
        // (merge, unlock, spawner) — later requests are dropped until it finishes.
        private static bool isNewItemToastActive;

        // Tutorial-only gate — while set, celebration floating texts ("New Item!", "Great!") are
        // skipped so they don't cover the tutorial's spotlight/bubble UI. Items still get marked
        // as seen (TryMarkItemSeen), so the toast won't re-fire for them after the tutorial.
        public static bool IsCelebrationTextSuppressed { get; private set; }

        public static void SetCelebrationTextSuppressed(bool suppressed) => IsCelebrationTextSuppressed = suppressed;

        // ─── Init ────────────────────────────────────────────────────────────────

        private MergeSave save;

        public void Init()
        {
            Instance = this;
            database.Init();

            canvas = mergeGrid.ObjectsContainer.GetComponentInParent<Canvas>();

            isNewItemToastActive = false;
            IsCelebrationTextSuppressed = false;
            SpawnerObject.SetEnergyVisualsSuppressed(false);
        }

        // Deferred to Start: CanvasScaler applies its scale factor in OnEnable, whose order
        // relative to this object's Awake is not guaranteed. MergeGrid sizes itself from
        // RectTransform.rect, which depends on that scale factor being already applied —
        // Start is guaranteed to run after every object's Awake/OnEnable in the scene.
        // The merge board is a single shared level (not per-zone) — built once at boot and
        // left untouched by zone switches.
        public void InitGrid()
        {
            save = SaveController.GetSaveObject<MergeSave>("MergeGrid");
            mergeGrid.Init(GameData.Data.LevelDatabase.MergeLevelData, database, save);
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        // Rebuilds save.Cells from the live grid and marks the file dirty. Called right after
        // every gameplay action that changes board state, so save.Cells never lags behind — a
        // save flush landing at an inconvenient time (e.g. mid-teardown, when grid occupants may
        // already be destroyed) always finds current data already sitting in save.Cells and has
        // nothing left to pull.
        public void SyncSaveData()
        {
            CollectSaveData(save);
            SaveController.MarkAsSaveIsRequired();
        }

        // Called by SyncSaveData() right after a gameplay action — rebuilds the full cell list
        // from the live grid. Only ever called while the grid is fully alive.
        public void CollectSaveData(MergeSave targetSave)
        {
            // Defensive: a flush landing before the grid is built must not wipe targetSave.Cells
            // down to an empty list — leave whatever was already saved untouched instead.
            if (mergeGrid.Width == 0 || mergeGrid.Height == 0)
                return;

            targetSave.Cells.Clear();

            for (int y = 0; y < mergeGrid.Height; y++)
            {
                for (int x = 0; x < mergeGrid.Width; x++)
                {
                    MergeFieldObject occupant = mergeGrid.GetCell(x, y).Occupant;
                    var data = new CellSaveData { X = x, Y = y };

                    if (occupant != null)
                    {
                        data.TypeId = occupant is LockedCell ? null : occupant.TypeId;
                        data.Grade = occupant.Grade;
                        data.IsHalfLocked = occupant.IsHalfLocked;
                        data.IsLocked = occupant is LockedCell;

                        object payload = occupant.OnBeforeSave();
                        if (payload != null)
                            data.CustomData = JsonUtility.ToJson(payload);
                    }

                    targetSave.Cells.Add(data);
                }
            }
        }

        // ─── Update ──────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!isDragging)
            {
                timeSinceLastMerge += Time.deltaTime;
                if (timeSinceLastMerge >= mergeHintDelay && !isShowingHint)
                    ShowMergeHint();
            }
        }

        // ─── Tap / Selection ─────────────────────────────────────────────────────

        public void HandleCellClick(MergeCellBackground cell)
        {
            MergeFieldObject obj = cell.Occupant;
            if (obj == null) { Deselect(); return; }

            if (obj == selectedObject)
            {
                // Re-tapping an already-selected object only fires actions safe to repeat
                // rapidly (e.g. Spawn). Destructive/economic actions (Sell, Delete, ...)
                // must go through the explicit action button in UISelectionPanel.
                if (obj.GetActionButton().ActivateOnTap)
                    ActivateSelected();
            }
            else
            {
                Select(obj);
            }
        }

        private void Select(MergeFieldObject obj)
        {
            // Selecting anything else forgets any pending Undo-delete.
            pendingDelete = null;

            if (selectedObject != null) selectedObject.OnDeselected();

            // Selecting one of the two hinted objects should cancel the hint immediately
            // rather than leaving it bouncing behind the selection highlight.
            HideMergeHint();

            selectedObject = obj;
            obj.OnSelected();
            MergeFieldObjectSelection.Instance?.Show(obj);
            OnObjectSelected?.Invoke(obj);
        }

        public void Deselect()
        {
            if (selectedObject == null) return;
            selectedObject.OnDeselected();
            selectedObject = null;
            MergeFieldObjectSelection.Instance?.Hide();
            OnObjectDeselected?.Invoke();
        }

        public void ActivateSelected()
        {
            if (selectedObject == null || selectedObject.IsHalfLocked)
            {
                return;
            }

            if (selectedObject is CurrencyItem || selectedObject is EnergyItem)
            {
                CollectItem(selectedObject);
                return;
            }

            switch (selectedObject.GetActionButton().Type)
            {
                case ActionButtonType.Delete: DeleteItem(selectedObject);  break;
                case ActionButtonType.Sell:   SellItem(selectedObject);    break;
                case ActionButtonType.Spawn:
                    if (selectedObject is SpawnerObject spawner)
                        SpawnerController.Instance?.TryActivate(spawner, FindPosition(selectedObject));
                    break;
            }
        }

        // ─── Drag ────────────────────────────────────────────────────────────────

        // Fired on touch/press, ahead of Unity's IBeginDragHandler (which only fires once the
        // pointer clears pixelDragThreshold) — keeps the pickup sound feeling instant.
        public void HandlePointerDown(MergeCellBackground cell)
        {
            MergeFieldObject obj = cell.Occupant;
            if (obj == null) return;
            if (!obj.IsVisualVisible) return;
            if (isDragLocked) return;
            if (!obj.CanDrag()) return;

            AudioController.PlaySound(AudioController.GetClip("item_pickup"));
        }

        public void BeginDrag(MergeCellBackground cell, PointerEventData eventData)
        {
            MergeFieldObject obj = cell.Occupant;
            if (obj == null) return;

            // Still mid-flight (spawn/displacement) — not visually at this cell yet, so it
            // must not be pickable, or the drag icon would duplicate the flying icon.
            if (!obj.IsVisualVisible) return;

            if (isDragLocked) return;

            if (!obj.CanDrag())
            {
                if (obj != selectedObject) Select(obj);
                return;
            }

            Select(obj);
            MergeFieldObjectSelection.Instance?.Hide();

            isDragging = true;
            draggedObject = obj;
            dragSourcePos = cell.Position;
            magnetTarget = null;
            magnetActiveCells.Clear();

            mergeGrid.GetCell(dragSourcePos).Occupant = null;
            ClientOrderHighlightController.Instance?.Refresh();
            BuildMagnetCandidates();

            obj.SetVisualVisible(false);

            dragStartScreenPos = eventData.position;
            dragStartIconPos = mergeGrid.GetIconAnchoredPosition(dragSourcePos) + new Vector2(0f, dragYOffset);

            dragIcon = mergeGrid.SpawnFlyingIcon();
            dragIcon.Show(obj, dragStartIconPos);
            dragIcon.SetScale(dragScale);
        }

        public void UpdateDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Recomputed from the absolute (clamped) screen position relative to drag start each
            // frame — never accumulated. eventData.delta is the raw OS pointer delta and can keep
            // reporting movement after eventData.position clamps at a screen edge, which would
            // make an accumulating sum drift away from where the cursor actually is.
            Vector2 screenDelta = eventData.position - dragStartScreenPos;
            Vector2 iconPos = dragStartIconPos + screenDelta / canvas.scaleFactor;
            dragIcon.SetAnchoredPosition(iconPos);

            UpdateMagnet(iconPos);
        }

        private void BuildMagnetCandidates()
        {
            magnetCandidates.Clear();
            for (int x = 0; x < mergeGrid.Width; x++)
            {
                for (int y = 0; y < mergeGrid.Height; y++)
                {
                    MergeCell cell = mergeGrid.GetCell(x, y);
                    if (cell.Occupant != null && CanMerge(draggedObject, cell.Occupant))
                        magnetCandidates.Add(cell);
                }
            }
        }

        private void UpdateMagnet(Vector2 iconPos)
        {
            float magnetRadius   = GameData.Data.LevelDatabase.MagnetRadius;
            float magnetStrength = GameData.Data.LevelDatabase.MagnetStrength;

            // magnetTarget's RectTransform is parented under objectsContainer, so its
            // anchoredPosition (and GetAnchoredPosition) is in that space — but iconPos is in
            // the drag icon's own parent space, offset from it by the constant IconAnchorOffset.
            // Comparing/lerping them directly shifts the effective radius off-center, making the
            // magnet trigger at different distances depending on approach direction.
            Vector2 iconPosInContainer = iconPos - mergeGrid.IconAnchorOffset;

            MergeCell nearest = null;
            float nearestDist = magnetRadius;

            foreach (MergeCell candidate in magnetCandidates)
            {
                Vector2 home = mergeGrid.GetAnchoredPosition(candidate.Position);
                float dist = Vector2.Distance(iconPosInContainer, home);

                if (dist <= magnetRadius)
                {
                    // Pull scales with proximity: barely moves right at the radius edge, approaches
                    // iconPos as the dragged item gets closer.
                    float pull = Mathf.Clamp01(1f - dist / magnetRadius) * magnetStrength;
                    ((RectTransform)candidate.Occupant.transform).anchoredPosition = Vector2.Lerp(home, iconPosInContainer, pull);
                    magnetActiveCells.Add(candidate);

                    if (dist <= nearestDist)
                    {
                        nearestDist = dist;
                        nearest = candidate;
                    }
                }
                else if (magnetActiveCells.Remove(candidate))
                {
                    ((RectTransform)candidate.Occupant.transform).anchoredPosition = home;
                }
            }

            magnetTarget = nearest?.Occupant;
            magnetTargetPos = nearest?.Position ?? default;
            magnetTargetDist = nearestDist;
        }

        private void ResetMagnetTargets()
        {
            foreach (MergeCell cell in magnetActiveCells)
            {
                if (cell.Occupant != null)
                    ((RectTransform)cell.Occupant.transform).anchoredPosition = mergeGrid.GetAnchoredPosition(cell.Position);
            }
            magnetActiveCells.Clear();
            magnetTarget = null;
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (!isDragging || draggedObject == null) return;
            isDragging = false;

            MergeFieldObject snappedTarget = magnetTarget != null && magnetTargetDist <= GameData.Data.LevelDatabase.MergeThreshold
                ? magnetTarget
                : null;
            Vector2Int snappedTargetPos = magnetTargetPos;
            ResetMagnetTargets();
            magnetCandidates.Clear();

            if (snappedTarget != null)
            {
                dragIcon.Release();
                ExecuteMerge(draggedObject, dragSourcePos, snappedTarget, snappedTargetPos);
                return;
            }

            MergeCellBackground targetCellBg = eventData.pointerCurrentRaycast.gameObject?.GetComponent<MergeCellBackground>();
            MergeCell targetCell = targetCellBg != null ? mergeGrid.GetCell(targetCellBg.Position) : null;

            if (targetCell != null)
            {
                if (targetCell.IsEmpty)
                {
                    dragIcon.Release();
                    MoveObject(draggedObject, targetCell.Position);
                    ResetMergeHint();
                    return;
                }

                if (CanMerge(draggedObject, targetCell.Occupant))
                {
                    dragIcon.Release();
                    ExecuteMerge(draggedObject, dragSourcePos, targetCell.Occupant, targetCell.Position);
                    return;
                }

                if (targetCell.State == CellState.Active && TryDisplaceAndPlace(draggedObject, targetCell))
                {
                    dragIcon.Release();
                    ResetMergeHint();
                    return;
                }
            }

            ReturnToSource();
        }

        private void MoveObject(MergeFieldObject obj, Vector2Int to)
        {
            obj.SetVisualVisible(true);
            mergeGrid.PlaceObject(obj, to.x, to.y);
            ClientOrderHighlightController.Instance?.Refresh();

            AudioController.PlaySound(AudioController.GetClip("item_place"));

            // obj may have been deselected mid-drag (e.g. a second touch tapping elsewhere) —
            // only re-show the highlight if it's still actually the selected object.
            if (selectedObject == obj)
                MergeFieldObjectSelection.Instance?.Show(obj);

            draggedObject = null;
            SyncSaveData();
        }

        private bool TryDisplaceAndPlace(MergeFieldObject obj, MergeCell targetCell)
        {
            MergeCell emptyNeighbor = null;
            foreach (MergeCell neighbor in mergeGrid.GetNeighbors8(targetCell.Position.x, targetCell.Position.y))
            {
                if (neighbor.IsEmpty) { emptyNeighbor = neighbor; break; }
            }
            if (emptyNeighbor == null) return false;

            MergeFieldObject displaced = targetCell.Occupant;
            Vector2Int toPos = emptyNeighbor.Position;
            emptyNeighbor.Occupant = displaced;

            Vector2 endAnchoredPos = mergeGrid.GetAnchoredPosition(toPos);

            displaced.SetVisualVisible(false);
            MergeFlyingIcon flyIcon = mergeGrid.SpawnFlyingIcon();
            flyIcon.Show(displaced, mergeGrid.GetIconAnchoredPosition(targetCell.Position));
            flyIcon.FlyTo(mergeGrid.GetIconAnchoredPosition(toPos), displaceFlightDuration, flightEase, () =>
            {
                if (displaced == null) return;

                ((RectTransform)displaced.transform).anchoredPosition = endAnchoredPos;
                displaced.SetVisualVisible(true);
                ClientOrderHighlightController.Instance?.Refresh();

                if (selectedObject == displaced)
                    MergeFieldObjectSelection.Instance?.Show(displaced);
            });

            MoveObject(obj, targetCell.Position);
            return true;
        }

        private void ReturnToSource()
        {
            if (draggedObject == null) return;

            MergeFieldObject obj = draggedObject;
            mergeGrid.GetCell(dragSourcePos).Occupant = obj;

            if (selectedObject == obj)
                MergeFieldObjectSelection.Instance?.Show(obj);

            draggedObject = null;

            dragIcon.FlyTo(dragStartIconPos, returnFlightDuration, flightEase, () =>
            {
                if (obj == null) return;

                obj.SetVisualVisible(true);
                ClientOrderHighlightController.Instance?.Refresh();
            });
        }

        // ─── Merge ───────────────────────────────────────────────────────────────

        private bool CanMerge(MergeFieldObject source, MergeFieldObject target)
        {
            if (source == null || target == null) return false;
            if (target is LockedCell) return false;
            if (source.TypeId != target.TypeId) return false;
            if (source.Grade != target.Grade) return false;

            MergeItemData data = database.GetItem(source.TypeId);
            return data != null && source.Grade < data.MaxGrade;
        }

        private void ExecuteMerge(MergeFieldObject source, Vector2Int sourcePos,
                                   MergeFieldObject target, Vector2Int targetPos)
        {
            string typeId = source.TypeId;
            int newGrade  = source.Grade + 1;

            // Only the merge that actually involves the current selection should hand
            // selection over to the result — otherwise this would override whatever the
            // player selected elsewhere while the drag was still in progress.
            bool reselect = selectedObject == source || selectedObject == target;
            if (reselect)
                Deselect();

            Destroy(source.gameObject);
            Destroy(target.gameObject);
            mergeGrid.GetCell(targetPos).Occupant = null;
            draggedObject = null;

            MergeItemData itemData = database.GetItem(typeId);
            MergeFieldObject merged = mergeGrid.SpawnObject(itemData, newGrade, targetPos.x, targetPos.y);
            if (reselect)
                Select(merged);

            Checkpoint.Log($"Merged {typeId} → grade {newGrade}", gameObject);

            OnMerged?.Invoke(merged);

            ParticlesController.PlayParticle("Merge")?.SetPosition(merged.transform.position)
                                                       .SetRotation(Quaternion.Euler(-180f, 0f, 0f));

            AudioController.PlaySound(AudioController.GetClip("item_merge"));

            UnlockNeighbors(targetPos);
            UnlockNeighbors(sourcePos);

            if (TryMarkItemSeen(typeId, newGrade))
                ShowNewItemFloatingText(merged.transform.position);

            ResetMergeHint();
            SyncSaveData();
        }

        private void UnlockNeighbors(Vector2Int pos)
        {
            foreach (MergeCell neighbor in mergeGrid.GetNeighbors(pos.x, pos.y))
            {
                if (neighbor.Occupant is not LockedCell lockedCell) continue;

                string hiddenTypeId = lockedCell.HiddenTypeId;
                int hiddenGrade     = lockedCell.HiddenGrade;
                Vector2Int nPos     = neighbor.Position;

                Destroy(lockedCell.gameObject);
                neighbor.Occupant = null;

                if (!string.IsNullOrEmpty(hiddenTypeId))
                {
                    MergeItemData data = database.GetItem(hiddenTypeId);
                    if (data != null)
                    {
                        MergeFieldObject unlocked = mergeGrid.SpawnObject(data, hiddenGrade, nPos.x, nPos.y, halfLocked: true);

                        if (TryMarkItemSeen(hiddenTypeId, hiddenGrade))
                            ShowNewItemFloatingText(unlocked.transform.position);
                    }
                }
            }
        }

        // ─── Item Actions ────────────────────────────────────────────────────────

        private void CollectItem(MergeFieldObject obj)
        {
            ResetMergeHint();

            Vector2Int pos = FindPosition(obj);

            if (obj is EnergyItem)
            {
                int amount = obj.GradeData?.GetConfig<EnergyGradeConfig>()?.Amount ?? 0;
                if (amount <= 0)
                    Debug.LogWarning($"[Merge] '{obj.TypeId}' grade {obj.Grade} has no EnergyGradeConfig (or amount is 0) — nothing will be credited. Fill the grade config in Merge Database.");

                AudioController.PlaySound(AudioController.GetClip("energy_pick_up"));

                mergeGrid.GetCell(pos).Occupant = null;
                mergeGrid.RaiseFieldChanged();

                ParticlesController.PlayParticle("Energy Item Usage")?.SetPosition(obj.transform.position);

                // Credit on first cloud element hit so the panel text updates when the cloud lands
                RectTransform energyText = UIController.GetPage<UIHeader>()?.EnergyPanel?.TextRectTransform;
                if (energyText != null)
                    CurrencyCloud.SpawnCurrency(EnergyUIPanel.CLOUD_KEY, (RectTransform)obj.transform, energyText, Mathf.Min(amount, 12), "", () => EnergyController.Add(amount, ignoreCap: true));
                else
                    EnergyController.Add(amount, ignoreCap: true);

                BounceAnimation.Bounce(obj.transform, GameData.Data.EnergyBounceSettings, () => { if (obj != null) Destroy(obj.gameObject); });
            }
            else if (obj is CurrencyItem)
            {
                CurrencyAmount reward = obj.GradeData?.GetConfig<CurrencyGradeConfig>()?.Amount;
                if (reward == null || reward.Amount <= 0)
                    Debug.LogWarning($"[Merge] '{obj.TypeId}' grade {obj.Grade} has no CurrencyGradeConfig (or amount is 0) — nothing will be credited. Fill the grade config in Merge Database.");

                mergeGrid.GetCell(pos).Occupant = null;
                mergeGrid.RaiseFieldChanged();

                ParticlesController.PlayParticle("Currency Item Usage")?.SetPosition(obj.transform.position);

                if (reward != null && reward.Amount > 0)
                {
                    UIHeader header = UIController.GetPage<UIHeader>();
                    RectTransform targetText = reward.CurrencyType switch
                    {
                        CurrencyType.Coins => header?.CoinsPanel?.TextRectTransform,
                        CurrencyType.Gems => header?.GemsPanel?.TextRectTransform,
                        _ => null
                    };

                    // Credit on first cloud element hit so the panel text updates when the cloud lands
                    if (targetText != null)
                        CurrencyCloud.SpawnCurrency(reward.CurrencyType.ToString(), (RectTransform)obj.transform, targetText, Mathf.Min(reward.Amount, 12), "", () => CurrencyController.Add(reward.CurrencyType, reward.Amount, "merge_pickup"));
                    else
                        CurrencyController.Add(reward.CurrencyType, reward.Amount, "merge_pickup");
                }

                BounceAnimation.Bounce(obj.transform, GameData.Data.CurrencyBounceSettings, () => { if (obj != null) Destroy(obj.gameObject); });
            }
            else
            {
                mergeGrid.RemoveObject(pos.x, pos.y);
            }

            Deselect();
            SyncSaveData();
        }

        private void DeleteItem(MergeFieldObject obj)
        {
            ResetMergeHint();

            Vector2Int pos = FindPosition(obj);
            Sprite icon = obj.CurrentSprite;

            mergeGrid.GetCell(pos).Occupant = null;
            mergeGrid.RaiseFieldChanged();
            selectedObject = null;
            MergeFieldObjectSelection.Instance?.Hide();

            pendingDelete = (obj.TypeId, obj.Grade, pos.x, pos.y);

            ParticlesController.PlayParticle("Delete")?.SetPosition(obj.transform.position);
            obj.transform.DOScale(Vector3.zero, deleteScaleDuration)
                .SetEasing(deleteScaleEase)
                .OnComplete(() => { if (obj != null) Destroy(obj.gameObject); });

            OnItemDeleted?.Invoke(icon);
            SyncSaveData();
        }

        public void UndoDelete()
        {
            if (pendingDelete == null) return;

            (string TypeId, int Grade, int X, int Y) info = pendingDelete.Value;
            pendingDelete = null;

            MergeItemData data = database.GetItem(info.TypeId);
            if (data == null) return;

            MergeFieldObject restored = mergeGrid.SpawnObject(data, info.Grade, info.X, info.Y);
            Select(restored);
            ParticlesController.PlayParticle("Appear")?.SetPosition(restored.transform.position);
            SyncSaveData();
        }

        private void SellItem(MergeFieldObject obj)
        {
            ResetMergeHint();

            Vector2Int pos = FindPosition(obj);
            int sellPrice = obj.GradeData?.GetConfig<ItemGradeConfig>()?.SellPrice ?? 0;

            mergeGrid.GetCell(pos).Occupant = null;
            mergeGrid.RaiseFieldChanged();

            if (sellPrice > 0)
            {
                RectTransform targetText = UIController.GetPage<UIHeader>()?.CoinsPanel?.TextRectTransform;

                // Credit on first cloud element hit so the panel text updates when the cloud lands
                if (targetText != null)
                    CurrencyCloud.SpawnCurrency(CurrencyType.Coins.ToString(), (RectTransform)obj.transform, targetText, Mathf.Min(sellPrice, 12), "", () => CurrencyController.Add(CurrencyType.Coins, sellPrice, "sell_item"));
                else
                    CurrencyController.Add(CurrencyType.Coins, sellPrice, "sell_item");
            }

            Deselect();
            Destroy(obj.gameObject);
            SyncSaveData();
        }

        // Removes a field object outside the normal action-button flow — used by task Give
        // handlers to consume the specific instances bound to a completed Client Order.
        public void RemoveFromGrid(MergeFieldObject obj)
        {
            if (obj == null) return;

            Vector2Int pos = FindPosition(obj);
            if (pos.x < 0) return;

            ResetMergeHint();
            if (selectedObject == obj) Deselect();
            mergeGrid.RemoveObject(pos.x, pos.y);
            SyncSaveData();
        }

        // Same as RemoveFromGrid, but the object scales out instead of vanishing in one frame —
        // used when an object removes itself (e.g. ChestObject running out of charges) rather
        // than a user-driven delete. Reuses DeleteItem's scale settings for a consistent feel.
        public void RemoveFromGridAnimated(MergeFieldObject obj)
        {
            if (obj == null) return;

            Vector2Int pos = FindPosition(obj);
            if (pos.x < 0) return;

            ResetMergeHint();
            if (selectedObject == obj) Deselect();

            mergeGrid.GetCell(pos).Occupant = null;
            mergeGrid.RaiseFieldChanged();
            SyncSaveData();

            obj.transform.DOScale(Vector3.zero, deleteScaleDuration)
                .SetEasing(deleteScaleEase)
                .OnComplete(() => { if (obj != null) Destroy(obj.gameObject); });
        }

        // ─── Merge Hint ──────────────────────────────────────────────────────────

        private void ResetMergeHint()
        {
            timeSinceLastMerge = 0f;
            HideMergeHint();
        }

        private void ShowMergeHint()
        {
            (MergeFieldObject a, MergeFieldObject b) = FindMergePair();
            if (a == null) return;

            isShowingHint = true;
            hintObjA = a;
            hintObjB = b;
            hintCoroutineA = StartCoroutine(BounceHintLoop(a));
            hintCoroutineB = StartCoroutine(BounceHintLoop(b));
        }

        private void HideMergeHint()
        {
            if (!isShowingHint) return;
            isShowingHint = false;

            if (hintCoroutineA != null) { StopCoroutine(hintCoroutineA); hintCoroutineA = null; }
            if (hintCoroutineB != null) { StopCoroutine(hintCoroutineB); hintCoroutineB = null; }

            if (hintObjA != null) { hintObjA.transform.localScale = Vector3.one; hintObjA = null; }
            if (hintObjB != null) { hintObjB.transform.localScale = Vector3.one; hintObjB = null; }
        }

        private IEnumerator BounceHintLoop(MergeFieldObject obj)
        {
            Vector3 original = obj.transform.localScale;
            Vector3 target   = original * hintBounceScale;
            const float halfPeriod = 0.35f;

            while (true)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t = Mathf.Min(t + Time.deltaTime / halfPeriod, 1f);
                    obj.transform.localScale = Vector3.Lerp(original, target, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                t = 0f;
                while (t < 1f)
                {
                    t = Mathf.Min(t + Time.deltaTime / halfPeriod, 1f);
                    obj.transform.localScale = Vector3.Lerp(target, original, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
            }
        }

        private (MergeFieldObject, MergeFieldObject) FindMergePair()
        {
            var seen = new Dictionary<string, MergeFieldObject>();

            for (int x = 0; x < mergeGrid.Width; x++)
            {
                for (int y = 0; y < mergeGrid.Height; y++)
                {
                    MergeCell cell = mergeGrid.GetCell(x, y);
                    if (cell.State != CellState.Active || !cell.Occupant.CanDrag()) continue;

                    MergeItemData data = database.GetItem(cell.Occupant.TypeId);
                    if (data == null || cell.Occupant.Grade >= data.MaxGrade) continue;

                    string key = $"{cell.Occupant.TypeId}_{cell.Occupant.Grade}";
                    if (seen.TryGetValue(key, out MergeFieldObject other))
                        return (other, cell.Occupant);

                    seen[key] = cell.Occupant;
                }
            }

            return (null, null);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        public bool IsGradeSeen(string typeId, int grade) => seenItemKeys.Contains($"{typeId}_{grade}");

        // Marks a grade as seen without the "New Item!" toast — used when discovery happens via
        // observation (UIInfoWindow finding a live unlocked instance) rather than a merge, so the
        // grade still reads as open later even after that instance is gone from the board.
        public void MarkGradeSeen(string typeId, int grade) => seenItemKeys.Add($"{typeId}_{grade}");

        // Marks a grade as seen and reports whether this is the first time — callers use this to
        // gate the "New Item!" toast (merge results, unlocked neighbors, spawner output).
        public bool TryMarkItemSeen(string typeId, int grade) => seenItemKeys.Add($"{typeId}_{grade}");

        public Vector2Int FindPosition(MergeFieldObject obj)
        {
            for (int x = 0; x < mergeGrid.Width; x++)
                for (int y = 0; y < mergeGrid.Height; y++)
                    if (mergeGrid.GetCell(x, y).Occupant == obj)
                        return new Vector2Int(x, y);

            return new Vector2Int(-1, -1);
        }

        public static void ShowNewItemFloatingText(Vector3 worldPos)
        {
            if (isNewItemToastActive || IsCelebrationTextSuppressed) return;

            FloatingTextBaseBehavior floatingText = FloatingTextController.SpawnFloatingText("new_item", "New Item!", worldPos);
            if (floatingText == null) return;

            isNewItemToastActive = true;
            floatingText.OnAnimationCompleted = () => isNewItemToastActive = false;
        }
    }
}
