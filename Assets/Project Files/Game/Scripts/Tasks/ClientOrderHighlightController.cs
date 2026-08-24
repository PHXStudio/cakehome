using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Highlights every live board cell whose occupant's (typeId, grade) is required by any
    // active Client Order — not just the one instance bound to a slot — so the player can see
    // every valid candidate at a glance. The highlight lives on the cell's static
    // MergeCellBackground rather than the item itself, so it never reacts to the item's own
    // scale animations (task bounce, merge hint) — it's a pure "this cell matters" marker.
    public class ClientOrderHighlightController : MonoBehaviour
    {
        [SerializeField] GameObject highlightPrefab;
        [SerializeField] float appearDuration = 0.25f;

        public static ClientOrderHighlightController Instance { get; private set; }

        private Pool highlightPool;
        private Transform poolContainer;

        private readonly Dictionary<Vector2Int, GameObject> activeHighlights = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<GameObject, TweenCase> appearTweens = new Dictionary<GameObject, TweenCase>();
        private readonly HashSet<(string typeId, int grade)> requiredKeys = new HashSet<(string, int)>();
        private readonly List<Vector2Int> staleScratch = new List<Vector2Int>();

        public void Init()
        {
            Instance = this;

            poolContainer = new GameObject("Highlight Pool").transform;
            poolContainer.SetParent(transform, false);

            highlightPool = new Pool(highlightPrefab, "ClientOrderHighlight", poolContainer);

            TaskController.OnTasksChanged += Refresh;
            MergeGrid.OnFieldChanged += Refresh;
        }

        private void OnDestroy()
        {
            TaskController.OnTasksChanged -= Refresh;
            MergeGrid.OnFieldChanged -= Refresh;
            Instance = null;
        }

        // Called after every board change (spawn/remove/merge via MergeGrid.OnFieldChanged,
        // order add/remove via TaskController.OnTasksChanged) and also explicitly by
        // MergeController at drag-lifecycle points that reposition an item without going
        // through either of those events (pickup, move, displace, return-to-source).
        public void Refresh()
        {
            if (MergeController.Instance == null || TaskController.Instance == null) return;

            MergeGrid grid = MergeController.Instance.Grid;

            requiredKeys.Clear();
            foreach (ITask task in TaskController.Instance.ActiveTasks)
            {
                if (task is not ClientOrderTask order) continue;

                foreach (OrderItem item in order.Items)
                    requiredKeys.Add((item.typeId, item.grade));
            }

            // Release highlights on cells that no longer hold a matching occupant.
            staleScratch.Clear();
            foreach (KeyValuePair<Vector2Int, GameObject> kvp in activeHighlights)
            {
                MergeFieldObject occupant = grid.GetCell(kvp.Key)?.Occupant;
                bool stillMatches = occupant != null && !occupant.IsHalfLocked && requiredKeys.Contains((occupant.TypeId, occupant.Grade));
                if (stillMatches) continue;

                Release(kvp.Value);
                staleScratch.Add(kvp.Key);
            }
            foreach (Vector2Int pos in staleScratch)
                activeHighlights.Remove(pos);

            // Attach highlights on cells with a newly matching occupant.
            if (requiredKeys.Count == 0) return;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (activeHighlights.ContainsKey(pos)) continue;

                    MergeFieldObject occupant = grid.GetCell(x, y).Occupant;
                    // Half-locked items can't be handed in — don't advertise them as candidates.
                    if (occupant == null || !occupant.IsVisualVisible || occupant.IsHalfLocked) continue;
                    if (!requiredKeys.Contains((occupant.TypeId, occupant.Grade))) continue;

                    MergeCellBackground background = grid.GetBackground(x, y);
                    if (background == null) continue;

                    GameObject highlight = highlightPool.GetPooledObject();
                    Attach(highlight, background);
                    activeHighlights[pos] = highlight;
                }
            }
        }

        private void Attach(GameObject highlight, MergeCellBackground background)
        {
            RectTransform rt = (RectTransform)highlight.transform;
            rt.SetParent(background.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            if (appearTweens.TryGetValue(highlight, out TweenCase activeTween))
                activeTween.KillActive();

            // Must never be a raycast target — it sits on top of MergeCellBackground, which owns
            // all pointer/drag input, and a raycastable highlight would swallow drops meant for it.
            Graphic graphic = highlight.GetComponent<Graphic>();
            graphic.raycastTarget = false;
            graphic.SetAlpha(0f);
            appearTweens[highlight] = graphic.DOFade(1f, appearDuration);
        }

        private void Release(GameObject highlight)
        {
            if (appearTweens.TryGetValue(highlight, out TweenCase activeTween))
            {
                activeTween.KillActive();
                appearTweens.Remove(highlight);
            }

            highlight.GetComponent<Graphic>().SetAlpha(1f);
            highlight.transform.SetParent(poolContainer, false);
            highlight.SetActive(false);
        }
    }
}
