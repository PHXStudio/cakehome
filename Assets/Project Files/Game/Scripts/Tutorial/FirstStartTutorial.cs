using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    // Onboarding flow for a brand-new player: intro dialog -> highlight the board button in the
    // Main Menu -> guided merges/spawner/order on the board -> two more injected orders (spawner
    // idle hint) -> back to the Main Menu -> build button -> first building upgrade -> once the
    // building popup and any completion dialog are closed, highlight the Main Menu back button
    // (same target as the very first step) so the player has an obvious way back to the board.
    // Parented under TutorialController in Game.unity (auto-discovered).
    public class FirstStartTutorial : BaseTutorial
    {
        public enum Step
        {
            IntroDialog,
            HighlightBoardButton,
            TeachFirstMerge,
            TeachGrayItemMerge,
            TeachChainMerge,
            TeachSpawnerTap,
            TeachSpawnedMerge,
            TeachGiveTask,
            SecondOrder,
            ThirdOrder,
            GoToMainMenu,
            HighlightBuildButton,
            TeachBuildingUpgrade,
            HighlightBackButton,
        }

        private const string STEP_SAVE_KEY = "FirstStartTutorial";

        [SerializeField] MergeGrid mergeGrid;

        [Header("Intro")]
        [SerializeField] DialogData introDialog;

        [Header("Heroine")]
        [SerializeField] CharacterData heroineCharacter;
        [SerializeField] EmotionType heroineEmotion = EmotionType.Default;

        [Header("Step 2 — Board Button")]
        [SerializeField, TextArea] string boardButtonHintText = "Let's head to the board and gather resources to put out the fire!";

        [Header("Injected Task")]
        [SerializeField] string taskId = "tutorial_first_order";
        [SerializeField] CharacterData taskCharacter;
        [SerializeField] List<OrderItem> taskItems = new List<OrderItem>();
        [SerializeField] int taskCoinsReward = 50;

        [Header("Step 3 — First Merge")]
        [SerializeField] Vector2Int[] step3Cells;
        [SerializeField, TextArea] string step3HintText = "Drag the two teapots together and see what happens!";

        [Header("Step 4 — Gray Item Merge")]
        [SerializeField] Vector2Int[] step4Cells;
        [SerializeField, TextArea] string step4HintText = "Gray items can't be moved, but can be merged. Give it a try!";

        [Header("Step 5 — Chain Merge")]
        [SerializeField] Vector2Int[] step5Cells;
        [SerializeField, TextArea] string step5HintText = "We've got a new item! Let's try to merge again!";

        [Header("Step 6/7 — Spawner")]
        [SerializeField] Vector2Int spawnerCell;
        [SerializeField] Vector2Int[] step6MaskCells;
        [SerializeField, TextArea] string step6HintText = "Look at this glowing teapot. It can produce items. Tap to try!";
        [SerializeField] OrderItem forcedSpawnItem;
        [SerializeField, TextArea] string step7HintText = "The black tea from the teapot can be merged as well. Give it a try!";

        [Header("Step 8 — Give Task")]
        [SerializeField, TextArea] string step8HintText = "Our target item is collected! Let's submit it now.";
        [SerializeField] float step8ZoneHighlightDelay = 1.5f;

        [Header("Step 9 — Second Order")]
        [SerializeField] string secondTaskId = "tutorial_second_order";
        [SerializeField] List<OrderItem> secondTaskItems = new List<OrderItem>();
        [SerializeField] int secondTaskCoinsReward = 50;

        [Header("Step 10 — Third Order")]
        [SerializeField] string thirdTaskId = "tutorial_third_order";
        [SerializeField] List<OrderItem> thirdTaskItems = new List<OrderItem>();
        [SerializeField] int thirdTaskCoinsReward = 50;

        [Header("Steps 9/10 — Spawner Hint")]
        [SerializeField] float spawnerHintDelay = 5f;

        [Header("Step 11 — Go To Main Menu")]
        [SerializeField, TextArea] string goToMenuHintText = "We have enough resources for an upgrade! Let's head back!";

        [Header("Step 13 — Building Upgrade")]
        [SerializeField] string targetBuildingId;

        [Header("Step 14 — Highlight Back Button")]
        [SerializeField, TextArea] string backButtonHintText = "Awesome! Now let's head back to the board!";

        private FirstStartTutorialSave stepSave;
        private Step? mergeAdvanceTarget;
        private Vector2Int? mergedItemCell;
        private UIClientOrderCard cachedGiveCard;
        private string awaitedOrderTaskId;
        private Step orderAdvanceTarget;
        private TweenCase spawnerHintTweenCase;
        private TweenCase giveHighlightDelayTweenCase;

        protected override void OnInitialised()
        {
            stepSave = SaveController.GetSaveObject<FirstStartTutorialSave>(STEP_SAVE_KEY);
        }

        protected override void OnStarted()
        {
            // Teapots on the board are SpawnerObjects — a tap would spawn a random item and
            // desync the scripted board the guided merge steps' cell configs assume.
            SpawnerController.SetSpawningLocked(stepSave.currentStep < Step.TeachSpawnerTap);
            SpawnerObject.SetEnergyVisualsSuppressed(stepSave.currentStep < Step.TeachSpawnerTap);
            MergeController.SetCelebrationTextSuppressed(true);

            // Header's store and energy-recovery entry points would pull the player away from the
            // scripted flow — locked for the whole tutorial, released in OnFinished.
            OpenStoreButton.SetLocked(true);
            EnergyUIPanel.SetAddButtonLocked(true);

            EnterStep(stepSave.currentStep);
        }

        protected override void OnFinished()
        {
            UnsubscribeAll();

            TutorialSpotlightMaskController.Hide();
            TutorialCanvasController.ResetTutorialCanvas();
            TutorialCanvasController.ResetPointer();
            UITutorialMessageBubble.Hide();
            SpawnerController.ClearForcedSpawn();
            SpawnerController.SetSpawningLocked(false);
            MergeController.SetDragLocked(false);
            SpawnerObject.SetEnergyVisualsSuppressed(false);
            MergeController.SetCelebrationTextSuppressed(false);
            OpenStoreButton.SetLocked(false);
            EnergyUIPanel.SetAddButtonLocked(false);

            // Progression tasks were held back for the whole tutorial (locked by GameController
            // at boot); release the queue now. Must run here: TaskController's own
            // OnUpgradeCompleted handler fires before this tutorial's (it subscribed earlier),
            // i.e. while the lock is still on.
            TaskController.SetProgressionLocked(false);

            // Same hold applies to the zone-start (atUpgrade: 0) progression reward/dialog —
            // granted now that onboarding is out of the way (see GameController.Start for the
            // already-completed boot path).
            BuildingController.TriggerInitialProgression(ZoneController.CurrentZone);
        }

        // Scene unload (TutorialController.OnDestroy) — static events (BuildingController,
        // UIController, ...) outlive this instance; without this, a stale handler fires on the
        // destroyed object after scene reload (MissingReferenceException in FinishTutorial).
        protected override void OnUnloaded()
        {
            UnsubscribeAll();
        }

        // Read directly from the save (safe before/without a live instance) so GameController can
        // decide, at boot, whether to open UIMainMenu (tutorial not yet reached the board) or
        // UIGame (finished, or already past that point — the rest of the tutorial resumes on the
        // board itself via the normal ActivateTutorial<FirstStartTutorial>()/OnStarted() path).
        public static bool ShouldOpenMainMenuAtBoot()
        {
            if (IsCompleted()) return false;

            FirstStartTutorialSave save = SaveController.GetSaveObject<FirstStartTutorialSave>(STEP_SAVE_KEY);
            return save.currentStep <= Step.HighlightBoardButton;
        }

        // Save-based (safe before/without a live instance) — used by TaskController to hold the
        // zone progression task queue until the whole tutorial is done.
        public static bool IsCompleted()
        {
            TutorialBaseSave tutorialSave = SaveController.GetSaveObject<TutorialBaseSave>(string.Format("TUTORIAL:{0}", nameof(FirstStartTutorial)));
            return tutorialSave.isFinished;
        }

        // ─── Step dispatch ───────────────────────────────────────────────────────

        private void EnterStep(Step step)
        {
            switch (step)
            {
                case Step.IntroDialog: Enter_IntroDialog(); break;
                case Step.HighlightBoardButton: Enter_HighlightBoardButton(); break;
                case Step.TeachFirstMerge: Enter_TeachFirstMerge(); break;
                case Step.TeachGrayItemMerge: Enter_TeachGrayItemMerge(); break;
                case Step.TeachChainMerge: Enter_TeachChainMerge(); break;
                case Step.TeachSpawnerTap: Enter_TeachSpawnerTap(); break;
                case Step.TeachSpawnedMerge: Enter_TeachSpawnedMerge(); break;
                case Step.TeachGiveTask: Enter_TeachGiveTask(); break;
                case Step.SecondOrder: Enter_SecondOrder(); break;
                case Step.ThirdOrder: Enter_ThirdOrder(); break;
                case Step.GoToMainMenu: Enter_GoToMainMenu(); break;
                case Step.HighlightBuildButton: Enter_HighlightBuildButton(); break;
                case Step.TeachBuildingUpgrade: Enter_TeachBuildingUpgrade(); break;
                case Step.HighlightBackButton: Enter_HighlightBackButton(); break;
            }
        }

        private void AdvanceTo(Step next)
        {
            stepSave.currentStep = next;
            SaveController.MarkAsSaveIsRequired();
            EnterStep(next);
        }

        // ─── Step 1 — Intro dialog ───────────────────────────────────────────────

        private void Enter_IntroDialog()
        {
            DialogController.Play(introDialog, () => AdvanceTo(Step.HighlightBoardButton));
        }

        // ─── Step 2 — Highlight board button ─────────────────────────────────────

        private void Enter_HighlightBoardButton()
        {
            EnsureTaskInjected();

            UIMainMenu menu = UIController.GetPage<UIMainMenu>();
            if (menu != null)
            {
                TutorialCanvasController.SetSiblingAbovePage(menu);

                RectTransform buttonRect = menu.BackButtonRectTransform;
                TutorialCanvasController.ActivateTutorialCanvas(buttonRect, true, true);
                TutorialCanvasController.ActivatePointer(buttonRect.position, TutorialPointerAnimations.POINTER_CLICK);
            }

            UITutorialMessageBubble.Show(heroineCharacter, heroineEmotion, boardButtonHintText, fixedOffsetY: 350f);

            UIController.PageOpened += OnPageOpenedForStep2;
        }

        private void EnsureTaskInjected()
        {
            if (!string.IsNullOrEmpty(stepSave.injectedTaskId)) return;

            ClientOrderTask task = TaskController.AddClientOrder(taskId, taskCharacter, taskItems, taskCoinsReward);
            stepSave.injectedTaskId = task.TaskId;
            SaveController.MarkAsSaveIsRequired();
        }

        private void OnPageOpenedForStep2(UIPage page, Type pageType)
        {
            if (pageType != typeof(UIGame)) return;

            UIController.PageOpened -= OnPageOpenedForStep2;

            TutorialCanvasController.ResetTutorialCanvas();
            TutorialCanvasController.ResetPointer();
            UITutorialMessageBubble.Hide();

            AdvanceTo(Step.TeachFirstMerge);
        }

        // ─── Steps 3/4/5 — Guided merges ─────────────────────────────────────────

        private void Enter_TeachFirstMerge() => ShowMergeStep(step3Cells, step3HintText, Step.TeachGrayItemMerge);
        private void Enter_TeachGrayItemMerge() => ShowMergeStep(step4Cells, step4HintText, Step.TeachChainMerge);
        private void Enter_TeachChainMerge() => ShowMergeStep(step5Cells, step5HintText, Step.TeachSpawnerTap);

        private void ShowMergeStep(Vector2Int[] cells, string hintText, Step nextStep)
        {
            TutorialCanvasController.SetSiblingAbovePage(UIController.GetPage<UIGame>());

            Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(mergeGrid, cells, TutorialSpotlightMaskController.RectTransform);
            TutorialSpotlightMaskController.Show(rect);
            ShowBottomBubble(hintText);

            if (TryFindMergeablePair(cells, out Vector3 a, out Vector3 b))
                TutorialCanvasController.ActivateDragLoopPointer(a, b);

            mergeAdvanceTarget = nextStep;
            MergeController.OnMerged += OnAnyMerged;
        }

        // Cell coordinates in step3Cells/step4Cells/step5Cells are authored against the board's
        // starting layout, but by step 4/5 earlier merges have already emptied the "source" cell
        // and moved the result elsewhere (ExecuteMerge always keeps the result at whichever cell
        // the player dropped on) — so a fixed index into that array can point at an empty cell.
        // Search the same candidate list for the pair that's actually mergeable right now.
        private bool TryFindMergeablePair(Vector2Int[] cells, out Vector3 a, out Vector3 b)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                MergeFieldObject first = mergeGrid.GetCell(cells[i])?.Occupant;
                if (first == null) continue;

                for (int j = i + 1; j < cells.Length; j++)
                {
                    MergeFieldObject second = mergeGrid.GetCell(cells[j])?.Occupant;
                    if (second == null || second.TypeId != first.TypeId || second.Grade != first.Grade) continue;

                    a = first.transform.position;
                    b = second.transform.position;
                    return true;
                }
            }

            a = Vector3.zero;
            b = Vector3.zero;
            return false;
        }

        private void OnAnyMerged(MergeFieldObject merged)
        {
            MergeController.OnMerged -= OnAnyMerged;

            mergedItemCell = FindCellOf(merged);

            if (mergeAdvanceTarget.HasValue)
                AdvanceTo(mergeAdvanceTarget.Value);
        }

        // ─── Steps 6/7 — Spawner ──────────────────────────────────────────────────

        private void Enter_TeachSpawnerTap()
        {
            TutorialCanvasController.SetSiblingAbovePage(UIController.GetPage<UIGame>());

            SpawnerController.SetSpawningLocked(false);
            SpawnerObject.SetEnergyVisualsSuppressed(false);

            List<Vector2Int> alreadySpawned = FindMatchingCellPositions(forcedSpawnItem);
            if (alreadySpawned.Count >= 2)
            {
                SpawnerController.ClearForcedSpawn();
                AdvanceTo(Step.TeachSpawnedMerge);
                return;
            }

            Vector2Int activeSpawnerCell = FindSpawnerCell() ?? spawnerCell;

            Vector2Int[] maskCells = step6MaskCells != null && step6MaskCells.Length > 0 ? step6MaskCells : new[] { activeSpawnerCell };
            Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(mergeGrid, maskCells, TutorialSpotlightMaskController.RectTransform);
            TutorialSpotlightMaskController.Show(rect);
            ShowBottomBubble(step6HintText);

            TutorialCanvasController.StopDragLoopPointer();
            TutorialCanvasController.ActivatePointer(GetOccupantWorldPosition(activeSpawnerCell), TutorialPointerAnimations.POINTER_CLICK);

            // Dragging is locked for this step so the merged spawner can't be moved (or merged
            // away) before the player taps it — HandleCellClick isn't gated by this lock, so
            // tapping to spawn still works. Later lookups (RestartSpawnerHintTimer) no longer
            // assume a fixed cell — they re-locate the spawner via FindSpawnerCell().
            MergeController.SetDragLocked(true);

            SpawnerController.SetForcedSpawn(forcedSpawnItem.typeId, forcedSpawnItem.grade);
            SpawnerController.OnSpawnCompleted += OnSpawnCompletedForStep6;
        }

        private void OnSpawnCompletedForStep6()
        {
            if (FindMatchingCellPositions(forcedSpawnItem).Count < 2) return;

            SpawnerController.OnSpawnCompleted -= OnSpawnCompletedForStep6;
            SpawnerController.ClearForcedSpawn();
            MergeController.SetDragLocked(false);

            AdvanceTo(Step.TeachSpawnedMerge);
        }

        private void Enter_TeachSpawnedMerge()
        {
            TutorialCanvasController.SetSiblingAbovePage(UIController.GetPage<UIGame>());

            List<Vector2Int> spawnedCells = FindMatchingCellPositions(forcedSpawnItem);

            Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(mergeGrid, spawnedCells, TutorialSpotlightMaskController.RectTransform);
            TutorialSpotlightMaskController.Show(rect);
            ShowBottomBubble(step7HintText);

            if (spawnedCells.Count >= 2)
                TutorialCanvasController.ActivateDragLoopPointer(GetOccupantWorldPosition(spawnedCells[0]), GetOccupantWorldPosition(spawnedCells[1]));

            mergeAdvanceTarget = Step.TeachGiveTask;
            MergeController.OnMerged += OnAnyMerged;
        }

        // ─── Step 8 — Submit the order ────────────────────────────────────────────

        private void Enter_TeachGiveTask()
        {
            // Dragging is locked for the whole step — the target item could otherwise be merged
            // away or displaced before it's submitted, permanently blocking progression.
            MergeController.SetDragLocked(true);

            // Merged item's cell known (normal flow, straight from step 7) — spotlight where it
            // landed first, then hand off to the Give button. On resume (fresh session already
            // saved at this step) the cell is lost, so skip straight to the button.
            if (mergedItemCell.HasValue)
            {
                TutorialCanvasController.SetSiblingAbovePage(UIController.GetPage<UIGame>());
                TutorialCanvasController.ResetPointer();

                Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(mergeGrid, new[] { mergedItemCell.Value }, TutorialSpotlightMaskController.RectTransform);
                TutorialSpotlightMaskController.Show(rect);
                ShowBottomBubble(step8HintText);

                giveHighlightDelayTweenCase = Tween.DelayedCall(step8ZoneHighlightDelay, ShowGiveButtonHighlight);
            }
            else
            {
                ShowGiveButtonHighlight();
            }
        }

        private void ShowGiveButtonHighlight()
        {
            UIClientOrderCard existing = UITaskPanel.FindOrderCard(taskId);
            if (existing != null)
                SetupGiveHighlight(existing);
            else
                UITaskPanel.OnClientOrderCardCreated += OnClientOrderCardCreatedForStep8;
        }

        private void OnClientOrderCardCreatedForStep8(ITask task, UIClientOrderCard card)
        {
            if (!(task is ClientOrderTask order) || order.TaskId != taskId) return;

            UITaskPanel.OnClientOrderCardCreated -= OnClientOrderCardCreatedForStep8;
            SetupGiveHighlight(card);
        }

        private void SetupGiveHighlight(UIClientOrderCard card)
        {
            cachedGiveCard = card;
            card.GiveButton.onClick.AddListener(OnGiveClickedForStep8);

            // Card just entered (fresh spawn, or a resume that re-created it on scene load) — its
            // enter animation tweens scale and preferred width up from 0 over EnterDuration, so
            // the button's RectTransform isn't at its final size/position yet. Measuring now would
            // spotlight a zero-width rect; wait for the animation to finish first.
            giveHighlightDelayTweenCase = Tween.DelayedCall(TaskCardAnimation.EnterDuration, () => ShowGiveHighlightVisuals(card));
        }

        private void ShowGiveHighlightVisuals(UIClientOrderCard card)
        {
            TutorialCanvasController.SetSiblingAbovePage(UIController.GetPage<UIGame>());

            RectTransform giveButtonRect = (RectTransform)card.GiveButton.transform;

            Rect rect = TutorialBoundsHelper.ComputeEncapsulatingRect(new[] { giveButtonRect }, TutorialSpotlightMaskController.RectTransform);
            TutorialSpotlightMaskController.Show(rect);
            TutorialCanvasController.StopDragLoopPointer();
            TutorialCanvasController.ActivatePointer(giveButtonRect.position, TutorialPointerAnimations.POINTER_CLICK);
            ShowBottomBubble(step8HintText);
        }

        private void OnGiveClickedForStep8()
        {
            if (cachedGiveCard != null)
                cachedGiveCard.GiveButton.onClick.RemoveListener(OnGiveClickedForStep8);

            MergeController.SetDragLocked(false);

            AdvanceTo(Step.SecondOrder);
        }

        // ─── Steps 9/10 — Extra orders (spawner idle hint) ────────────────────────

        private void Enter_SecondOrder() => ShowSpawnerOrderStep(secondTaskId, secondTaskItems, secondTaskCoinsReward, ref stepSave.injectedTaskId2, Step.ThirdOrder);
        private void Enter_ThirdOrder() => ShowSpawnerOrderStep(thirdTaskId, thirdTaskItems, thirdTaskCoinsReward, ref stepSave.injectedTaskId3, Step.GoToMainMenu);

        private void ShowSpawnerOrderStep(string orderId, List<OrderItem> items, int coinsReward, ref string injectedIdSlot, Step nextStep)
        {
            TutorialSpotlightMaskController.Hide();
            UITutorialMessageBubble.Hide();
            TutorialCanvasController.ResetPointer();

            if (string.IsNullOrEmpty(injectedIdSlot))
            {
                TaskController.AddClientOrder(orderId, taskCharacter, items, coinsReward);
                injectedIdSlot = orderId;
                SaveController.MarkAsSaveIsRequired();
            }
            else if (!IsOrderActive(orderId))
            {
                // Injected on a previous session and already submitted — nothing left to wait for.
                AdvanceTo(nextStep);
                return;
            }

            awaitedOrderTaskId = orderId;
            orderAdvanceTarget = nextStep;

            RestartSpawnerHintTimer();
            SpawnerController.OnSpawnCompleted += OnSpawnCompletedForOrderStep;
            TaskController.OnTasksChanged += OnTasksChangedForOrderStep;
        }

        private void RestartSpawnerHintTimer()
        {
            spawnerHintTweenCase.KillActive();
            spawnerHintTweenCase = Tween.DelayedCall(spawnerHintDelay, () =>
            {
                TutorialCanvasController.SetSiblingAbovePage(UIController.GetPage<UIGame>());
                TutorialCanvasController.ActivatePointer(GetOccupantWorldPosition(FindSpawnerCell() ?? spawnerCell), TutorialPointerAnimations.POINTER_CLICK);
            });
        }

        private void OnSpawnCompletedForOrderStep()
        {
            TutorialCanvasController.ResetPointer();
            RestartSpawnerHintTimer();
        }

        private void OnTasksChangedForOrderStep()
        {
            if (IsOrderActive(awaitedOrderTaskId)) return;

            TaskController.OnTasksChanged -= OnTasksChangedForOrderStep;
            SpawnerController.OnSpawnCompleted -= OnSpawnCompletedForOrderStep;
            spawnerHintTweenCase.KillActive();
            TutorialCanvasController.ResetPointer();

            AdvanceTo(orderAdvanceTarget);
        }

        private static bool IsOrderActive(string orderId)
        {
            if (TaskController.Instance == null) return false;

            IReadOnlyList<ITask> tasks = TaskController.Instance.ActiveTasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].TaskId == orderId)
                    return true;
            }

            return false;
        }

        // ─── Step 11 — Go to main menu ────────────────────────────────────────────

        private void Enter_GoToMainMenu()
        {
            UIMainMenu menu = UIController.GetPage<UIMainMenu>();
            if (menu != null && menu.IsPageDisplayed)
            {
                AdvanceTo(Step.HighlightBuildButton);
                return;
            }

            // Fixed offset, not ShowBottomBubble — anchoring below the board here lands right on
            // top of the map button in the bottom nav bar.
            UITutorialMessageBubble.Show(heroineCharacter, heroineEmotion, goToMenuHintText, fixedOffsetY: 350f);
            HighlightMapButton();

            UIController.PageOpened += OnPageOpenedForMainMenuStep;
        }

        // Deferred one frame: PageOpened fires from inside UIController.ShowPage BEFORE
        // EnableCanvas sets IsPageDisplayed, so a same-frame check would false-negative.
        private void HighlightMapButton()
        {
            Tween.NextFrame(() =>
            {
                if (stepSave.currentStep != Step.GoToMainMenu) return;

                UIGame gamePage = UIController.GetPage<UIGame>();
                if (gamePage == null || !gamePage.IsPageDisplayed) return;

                TutorialCanvasController.SetSiblingAbovePage(gamePage);

                RectTransform buttonRect = gamePage.MapButtonRectTransform;
                TutorialCanvasController.ActivateTutorialCanvas(buttonRect, true, true);
                TutorialCanvasController.ActivatePointer(buttonRect.position, TutorialPointerAnimations.POINTER_CLICK);
            });
        }

        private void OnPageOpenedForMainMenuStep(UIPage page, Type pageType)
        {
            if (pageType == typeof(UIGame))
            {
                // Resume path: the board page wasn't displayed yet when the step entered.
                HighlightMapButton();
                return;
            }

            if (pageType != typeof(UIMainMenu)) return;

            UIController.PageOpened -= OnPageOpenedForMainMenuStep;

            TutorialCanvasController.ResetTutorialCanvas();
            TutorialCanvasController.ResetPointer();
            UITutorialMessageBubble.Hide();

            AdvanceTo(Step.HighlightBuildButton);
        }

        // ─── Step 12 — Build button on main menu ─────────────────────────────────

        private void Enter_HighlightBuildButton()
        {
            HighlightBuildButton();

            UIController.PageOpened += OnPageOpenedForBuildButtonStep;
        }

        // Deferred one frame — see HighlightMapButton for why.
        private void HighlightBuildButton()
        {
            Tween.NextFrame(() =>
            {
                if (stepSave.currentStep != Step.HighlightBuildButton) return;

                UIMainMenu menu = UIController.GetPage<UIMainMenu>();
                if (menu == null || !menu.IsPageDisplayed) return;

                TutorialCanvasController.SetSiblingAbovePage(menu);

                RectTransform buttonRect = menu.BuildButtonRectTransform;
                TutorialCanvasController.ActivateTutorialCanvas(buttonRect, true, true);
                TutorialCanvasController.ActivatePointer(buttonRect.position, TutorialPointerAnimations.POINTER_CLICK);
            });
        }

        private void OnPageOpenedForBuildButtonStep(UIPage page, Type pageType)
        {
            if (pageType == typeof(UIMainMenu))
            {
                // Player navigated back to the menu (or it opened late on resume) — re-highlight.
                HighlightBuildButton();
                return;
            }

            if (pageType != typeof(UIBuilding)) return;

            UIController.PageOpened -= OnPageOpenedForBuildButtonStep;

            TutorialCanvasController.ResetTutorialCanvas();
            TutorialCanvasController.ResetPointer();

            AdvanceTo(Step.TeachBuildingUpgrade);
        }

        // ─── Step 13 — First building upgrade ─────────────────────────────────────

        private void Enter_TeachBuildingUpgrade()
        {
            BuildingController.OnUpgradeCompleted += OnUpgradeCompletedForUpgradeStep;
            UIController.PageOpened += OnPageOpenedForUpgradeStep;

            TryHighlightTargetBuilding();
        }

        private void OnPageOpenedForUpgradeStep(UIPage page, Type pageType)
        {
            if (pageType == typeof(UIBuilding))
            {
                TryHighlightTargetBuilding();
            }
            else
            {
                // Player left the building page — the spotlight would otherwise keep pointing at
                // a hidden card's rect (mask hole over nothing).
                TutorialSpotlightMaskController.Hide();
                TutorialCanvasController.ResetPointer();
            }
        }

        // Deferred one frame — see HighlightMapButton for why.
        private void TryHighlightTargetBuilding()
        {
            Tween.NextFrame(() =>
            {
                if (stepSave.currentStep != Step.TeachBuildingUpgrade) return;

                UIBuilding buildingPage = UIController.GetPage<UIBuilding>();
                if (buildingPage == null || !buildingPage.IsPageDisplayed) return;

                BuildingUIBehavior card = UIBuilding.FindCard(targetBuildingId);
                if (card == null) return;

                TutorialCanvasController.SetSiblingAbovePage(buildingPage);

                RectTransform rect = card.BuildButtonRectTransform;
                Rect maskRect = TutorialBoundsHelper.ComputeEncapsulatingRect(new[] { rect }, TutorialSpotlightMaskController.RectTransform);
                TutorialSpotlightMaskController.Show(maskRect);
                TutorialCanvasController.ActivatePointer(rect.position, TutorialPointerAnimations.POINTER_CLICK);
            });
        }

        // Accepts any building's upgrade, not just targetBuildingId — the upgrade button isn't
        // locked for other buildings (affordability is the only gate), so a player who upgrades
        // a different one first would otherwise never clear this step, permanently soft-locking
        // zone progression (SetProgressionLocked(false) only runs in OnFinished).
        private void OnUpgradeCompletedForUpgradeStep(ZoneData zone, string buildingId, int totalUpgradesInZone)
        {
            BuildingController.OnUpgradeCompleted -= OnUpgradeCompletedForUpgradeStep;
            UIController.PageOpened -= OnPageOpenedForUpgradeStep;

            TutorialSpotlightMaskController.Hide();
            TutorialCanvasController.ResetPointer();

            // OnUpgradeCompleted fires before BuildingController enqueues its completion-thought/
            // progression dialog, so advancing straight away would race it — the dialog could pop
            // up after this step already started highlighting. Wait for the UI queue to fully
            // drain (dialog shown and dismissed) before moving on.
            UIQueueTail.WaitForIdle(() => AdvanceTo(Step.HighlightBackButton));
        }

        // ─── Step 14 — Highlight back button ──────────────────────────────────────

        // By the time this step is entered the queued dialog has already been shown and
        // dismissed (see UIQueueTail.WaitForIdle above) — this only still needs to wait for the
        // player to close the UIBuilding popup itself before pointing them back to the board,
        // same target button as Step 2.
        //
        // Resume/already-there check mirrors Enter_GoToMainMenu: a player who backs out of
        // UIBuilding straight to the board while WaitForIdle was still waiting on the queue
        // arrives here with UIGame already open — no PageOpened(UIGame) will fire again to
        // trigger OnPageOpenedForBackButtonStep's finish path, which would otherwise soft-lock
        // the tutorial (and the progression/store/energy locks held until OnFinished) forever.
        private void Enter_HighlightBackButton()
        {
            UIGame gamePage = UIController.GetPage<UIGame>();
            if (gamePage != null && gamePage.IsPageDisplayed)
            {
                FinishTutorial();
                return;
            }

            UIController.PageOpened += OnPageOpenedForBackButtonStep;
            UIController.PageClosed += OnPageClosedForBackButtonStep;

            TryHighlightBackButton();
        }

        private void TryHighlightBackButton()
        {
            if (stepSave.currentStep != Step.HighlightBackButton) return;
            if (UIController.IsDisplayed<UIBuilding>()) return;
            if (UIController.IsDisplayed<UIDialog>()) return;

            UIMainMenu menu = UIController.GetPage<UIMainMenu>();
            if (menu == null || !menu.IsPageDisplayed) return;

            TutorialCanvasController.SetSiblingAbovePage(menu);

            RectTransform buttonRect = menu.BackButtonRectTransform;
            TutorialCanvasController.ActivateTutorialCanvas(buttonRect, true, true);
            TutorialCanvasController.ActivatePointer(buttonRect.position, TutorialPointerAnimations.POINTER_CLICK);
            UITutorialMessageBubble.Show(heroineCharacter, heroineEmotion, backButtonHintText, fixedOffsetY: 350f);
        }

        private void OnPageClosedForBackButtonStep(UIPage page, Type pageType)
        {
            TryHighlightBackButton();
        }

        // Also handles the resume path: if the app reopens straight onto the board, the player
        // is already where this step wants them — finish immediately instead of waiting.
        private void OnPageOpenedForBackButtonStep(UIPage page, Type pageType)
        {
            if (pageType != typeof(UIGame))
            {
                TryHighlightBackButton();
                return;
            }

            UIController.PageOpened -= OnPageOpenedForBackButtonStep;
            UIController.PageClosed -= OnPageClosedForBackButtonStep;

            TutorialCanvasController.ResetTutorialCanvas();
            TutorialCanvasController.ResetPointer();
            UITutorialMessageBubble.Hide();

            FinishTutorial();
        }

        // ─── Shared helpers ───────────────────────────────────────────────────────

        // Anchors the bubble below the board's own container instead of its fixed default
        // position — on tablets, the board's visible bottom edge sits lower than on phone aspect
        // ratios, and the fixed offset let it overlap. Used by every hint shown while the board
        // (UIGame) is on screen; steps that show on Main Menu keep the default position.
        private void ShowBottomBubble(string hintText)
        {
            UITutorialMessageBubble.Show(heroineCharacter, heroineEmotion, hintText, mergeGrid.ObjectsContainer, 30f);
        }

        private Vector3 GetOccupantWorldPosition(Vector2Int cell)
        {
            MergeFieldObject occupant = mergeGrid.GetCell(cell)?.Occupant;
            return occupant != null ? occupant.transform.position : Vector3.zero;
        }

        private Vector2Int? FindCellOf(MergeFieldObject obj)
        {
            for (int y = 0; y < mergeGrid.Height; y++)
            {
                for (int x = 0; x < mergeGrid.Width; x++)
                {
                    if (mergeGrid.GetCell(x, y)?.Occupant == obj)
                        return new Vector2Int(x, y);
                }
            }

            return null;
        }

        // The steps 3-5 merge chain can leave the resulting spawner on any of its candidate
        // cells depending on which way the player actually drags (ExecuteMerge always keeps the
        // result at the drop target — see TryFindMergeablePair above for the same drift on the
        // merge steps) — search the live grid instead of trusting the authored spawnerCell.
        // Half-locked occupants are skipped: ExecuteMerge's UnlockNeighbors reveals adjacent
        // locked cells (as half-locked, "gray" items — see step4HintText) after every merge, and
        // those can include decoy SpawnerObject instances that aren't the chain's actual result.
        private Vector2Int? FindSpawnerCell()
        {
            for (int y = 0; y < mergeGrid.Height; y++)
            {
                for (int x = 0; x < mergeGrid.Width; x++)
                {
                    if (mergeGrid.GetCell(x, y)?.Occupant is SpawnerObject spawner && !spawner.IsHalfLocked)
                        return new Vector2Int(x, y);
                }
            }

            return null;
        }

        private List<Vector2Int> FindMatchingCellPositions(OrderItem item)
        {
            List<Vector2Int> result = new List<Vector2Int>();

            for (int y = 0; y < mergeGrid.Height; y++)
            {
                for (int x = 0; x < mergeGrid.Width; x++)
                {
                    MergeFieldObject occupant = mergeGrid.GetCell(x, y)?.Occupant;
                    if (occupant != null && !occupant.IsHalfLocked && occupant.TypeId == item.typeId && occupant.Grade == item.grade)
                        result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }

        private void UnsubscribeAll()
        {
            UIController.PageOpened -= OnPageOpenedForStep2;
            UIController.PageOpened -= OnPageOpenedForMainMenuStep;
            UIController.PageOpened -= OnPageOpenedForBuildButtonStep;
            UIController.PageOpened -= OnPageOpenedForUpgradeStep;
            UIController.PageClosed -= OnPageClosedForBackButtonStep;
            UIController.PageOpened -= OnPageOpenedForBackButtonStep;
            MergeController.OnMerged -= OnAnyMerged;
            SpawnerController.OnSpawnCompleted -= OnSpawnCompletedForStep6;
            SpawnerController.OnSpawnCompleted -= OnSpawnCompletedForOrderStep;
            TaskController.OnTasksChanged -= OnTasksChangedForOrderStep;
            UITaskPanel.OnClientOrderCardCreated -= OnClientOrderCardCreatedForStep8;
            BuildingController.OnUpgradeCompleted -= OnUpgradeCompletedForUpgradeStep;

            spawnerHintTweenCase.KillActive();
            giveHighlightDelayTweenCase.KillActive();

            if (cachedGiveCard != null)
                cachedGiveCard.GiveButton.onClick.RemoveListener(OnGiveClickedForStep8);
        }
    }
}
