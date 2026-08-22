using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Single card for the whole spawner reward queue — UITaskPanel re-Setups it with the
    // queue's top entry after every claim and destroys it once the queue drains.
    public class UISpawnerRewardCard : MonoBehaviour, ITaskCardView
    {
        [SerializeField] Image spawnerIcon;
        [SerializeField] Button claimButton;
        [SerializeField] LayoutElement layoutElement;

        [Header("UI Flight")]
        [SerializeField] MergeFlyingIcon.SimpleFlightSettings uiFlightSettings;
        [SerializeField] SimpleBounce iconBounce;

        private string spawnerTypeId;
        private int spawnerGrade;
        private Action onClaimed;
        private float defaultPreferredWidth;

        public Button ClaimButton => claimButton;

        private void Awake()
        {
            iconBounce.Init(spawnerIcon.transform);
            defaultPreferredWidth = layoutElement.preferredWidth;
        }

        public void Setup(SpawnerQueueEntry entry, Action onClaimedCallback)
        {
            // Re-Setup means the shown entry changed (next after a claim, or a new push on
            // top of the queue) — bounce the icon as feedback for the swap.
            bool entryChanged = spawnerTypeId != null;

            spawnerTypeId = entry.TypeId;
            spawnerGrade  = Mathf.Max(1, entry.Grade);
            onClaimed     = onClaimedCallback;

            Sprite sprite = MergeDatabase.Instance?.GetItem(spawnerTypeId)?.GetGradeData(spawnerGrade)?.Sprite;
            if (sprite != null) spawnerIcon.sprite = sprite;

            claimButton.interactable = true;

            if (entryChanged) iconBounce.Bounce();
        }

        public void OnClaimClicked()
        {
            claimButton.interactable = false;

            MergeController controller = MergeController.Instance;
            MergeGrid grid = controller?.Grid;
            MergeItemData data = MergeDatabase.Instance?.GetItem(spawnerTypeId);

            if (data == null)
            {
                // Misconfigured reward (unknown/empty typeId) — the entry can never spawn, so
                // drop it instead of jamming the queue behind a card that always fails.
                Debug.LogError($"[UISpawnerRewardCard] Unknown spawner typeId '{spawnerTypeId}' — dropping queue entry, check the reward config.");
                onClaimed?.Invoke();
                return;
            }

            MergeCell cell = grid?.FindAnyEmpty();
            if (grid == null || cell == null)
            {
                // No room to place the reward — leave the card up so the player can retry later.
                claimButton.interactable = true;
                FloatingTextController.SpawnFloatingText("spawner_hint", "No Space!", spawnerIcon.transform.position);
                return;
            }

            MergeFieldObject spawned = grid.SpawnObject(data, spawnerGrade, cell.Position.x, cell.Position.y, startVisible: false);
            FlyToTarget(grid, spawned, cell.Position);

            // Same as SpawnerController.TryActivate — the placed object must hit the merge save
            // together with the queue pop, or a kill right after the click would desync them.
            controller.SyncSaveData();

            // Panel pops the queue and either re-Setups this card with the next entry
            // or plays its exit — the card never destroys itself.
            onClaimed?.Invoke();
        }

        private void FlyToTarget(MergeGrid grid, MergeFieldObject spawned, Vector2Int targetPos)
        {
            Vector2 sourcePos = grid.WorldToIconAnchoredPosition(spawnerIcon.transform.position);

            MergeFlyingIcon flyIcon = grid.SpawnFlyingIcon();
            flyIcon.Show(spawned, sourcePos);

            AudioController.PlaySound(AudioController.GetClip("item_flying"));

            flyIcon.FlyTo(grid.GetIconAnchoredPosition(targetPos), uiFlightSettings.Duration, uiFlightSettings.Easing, () =>
            {
                if (spawned == null) return;
                spawned.SetVisualVisible(true);
                ClientOrderHighlightController.Instance?.Refresh();
                ParticlesController.PlayParticle("Appear")?.SetPosition(spawned.transform.position);
            });
        }

        public void PlayEnter() => TaskCardAnimation.PlayEnter(transform, layoutElement, defaultPreferredWidth);
        public void PlayExit(Action onComplete) => TaskCardAnimation.PlayExit(transform, layoutElement, onComplete);
    }
}
