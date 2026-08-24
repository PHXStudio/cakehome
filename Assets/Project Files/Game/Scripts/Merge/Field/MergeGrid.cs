using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class MergeGrid : MonoBehaviour
    {
        [SerializeField] RectTransform objectsContainer;
        [SerializeField] RectTransform itemsContainer;
        [SerializeField] RectTransform flyingIconsContainer;
        [SerializeField] GameObject lockedCellPrefab;

        private GridLayoutGroup gridLayout;
        private Vector2 cellSize;
        private Vector2 spacing;
        private Vector2 paddingTopLeft;

        private MergeCell[,] cells;
        private MergeCellBackground[,] backgrounds;
        private MergeDatabase database;
        private Pool flyingIconPool;
        private Transform flyingIconParent;

        public int Width { get; private set; }
        public int Height { get; private set; }

        // Fired after any change to the set of objects occupying the grid (spawn or remove —
        // covers merges too, since a merge always ends with SpawnObject for the result). Tasks
        // listen to this to re-scan field presence rather than relying on one-shot notifications.
        public static event Action OnFieldChanged;

        public RectTransform ObjectsContainer => objectsContainer;
        public RectTransform ItemsContainer => itemsContainer;
        public RectTransform FlyingIconsContainer => flyingIconsContainer;

        // Constant offset between objectsContainer's anchoredPosition space (used by
        // GetAnchoredPosition) and the flying icon's parent's anchoredPosition space.
        public Vector2 IconAnchorOffset { get; private set; }

        public Vector2 GetIconAnchoredPosition(Vector2Int pos) => GetAnchoredPosition(pos) + IconAnchorOffset;

        public void Init(MergeLevelData levelData, MergeDatabase database, MergeSave save = null)
        {
            this.database = database;
            Width = levelData.GridWidth;
            Height = levelData.GridHeight;

            cells = new MergeCell[Width, Height];
            backgrounds = new MergeCellBackground[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    cells[x, y] = new MergeCell { Position = new Vector2Int(x, y) };

            SetupLayout();
            SpawnCellBackgrounds();
            SpawnFlyingIconPool();

            Dictionary<Vector2Int, CellSaveData> saveByPos = null;
            if (save != null && save.Cells.Count > 0)
            {
                saveByPos = new Dictionary<Vector2Int, CellSaveData>(save.Cells.Count);
                foreach (CellSaveData cellData in save.Cells)
                    saveByPos[new Vector2Int(cellData.X, cellData.Y)] = cellData;
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (saveByPos != null && saveByPos.TryGetValue(new Vector2Int(x, y), out CellSaveData cellData))
                    {
                        SpawnFromSave(cellData, x, y);
                        continue;
                    }

                    CellConfig config = levelData.GetCell(x, y);
                    if (!config.IsEmpty)
                        SpawnFromConfig(config, x, y);
                }
            }
        }

        // Adapts GridLayoutGroup.cellSize so Width x Height cells exactly fill the container,
        // then disables it — actual placement is driven by GetAnchoredPosition (supports gaps/dragging).
        private void SetupLayout()
        {
            gridLayout = objectsContainer.GetComponent<GridLayoutGroup>();

            spacing = gridLayout.spacing;

            Rect rect = objectsContainer.rect;
            cellSize = new Vector2(
                (rect.width - spacing.x * (Width - 1)) / Width,
                (rect.height - spacing.y * (Height - 1)) / Height);

            gridLayout.cellSize = cellSize;
            gridLayout.enabled = false;
        }

        private void SpawnCellBackgrounds()
        {
            Color colorA = GameData.Data.LevelDatabase.CellColorA;
            Color colorB = GameData.Data.LevelDatabase.CellColorB;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    GameObject go = new GameObject("Cell Background", typeof(RectTransform), typeof(Image), typeof(MergeCellBackground));
                    go.transform.SetParent(objectsContainer, false);
                    go.GetComponent<Image>().color = (x + y) % 2 == 0 ? colorA : colorB;

                    MergeCellBackground background = go.GetComponent<MergeCellBackground>();
                    background.Init(cells[x, y]);
                    backgrounds[x, y] = background;

                    PlaceInCell(go.GetComponent<RectTransform>(), x, y);
                }
            }
        }

        private void SpawnFlyingIconPool()
        {
            GameObject template = new GameObject("Merge Flying Icon", typeof(RectTransform));
            RectTransform rectTransform = template.GetComponent<RectTransform>();

            // Shadow then Icon, both children of the root — MergeFlyingIcon finds them by name
            // ("Shadow"/"Icon") and moves/scales with the root automatically. Shadow is added
            // first so it draws behind Icon (uGUI draws later siblings on top), and mirrors the
            // flying item's shadow so it travels with the icon instead of being left static on
            // the field (see MergeFlyingIcon.Show(MergeFieldObject, Vector2)).
            GameObject shadowGo = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            RectTransform shadowRect = shadowGo.GetComponent<RectTransform>();
            shadowGo.transform.SetParent(rectTransform, false);
            shadowRect.anchorMin = shadowRect.anchorMax = shadowRect.pivot = new Vector2(0.5f, 0.5f);

            Image shadowImage = shadowGo.GetComponent<Image>();
            shadowImage.raycastTarget = false;
            shadowImage.enabled = false;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconGo.transform.SetParent(rectTransform, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            // Add MergeFlyingIcon only after Icon/Shadow children exist — AddComponent calls
            // Awake() synchronously, and Awake looks up those children by name.
            template.AddComponent<MergeFlyingIcon>();

            // Spawn under objectsContainer first so anchoredPosition (0,0) lines up with
            // GetAnchoredPosition's coordinate space, then move it out (preserving world
            // position) so the mask doesn't clip it while flying. The anchoredPosition
            // left behind by the reparent is the constant offset between the two spaces.
            template.transform.SetParent(objectsContainer, false);
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = cellSize;
            rectTransform.anchoredPosition = Vector2.zero;

            template.transform.SetParent(flyingIconsContainer, true);
            IconAnchorOffset = rectTransform.anchoredPosition;
            flyingIconParent = flyingIconsContainer;

            template.SetActive(false);
            flyingIconPool = new Pool(template, "MergeFlyingIcon", flyingIconsContainer);
        }

        public MergeFlyingIcon SpawnFlyingIcon() => flyingIconPool.GetPooledComponent<MergeFlyingIcon>();

        private void SpawnFromConfig(CellConfig config, int x, int y)
        {
            if (config.IsLocked)
            {
                LockedCell lc = SpawnLockedCell(x, y);
                lc.HiddenTypeId = config.TypeId;
                lc.HiddenGrade = config.Grade;
                return;
            }

            MergeItemData data = database.GetItem(config.TypeId);
            if (data == null)
            {
                Debug.LogWarning($"MergeGrid: unknown typeId '{config.TypeId}' at ({x},{y})");
                return;
            }

            SpawnObject(data, config.Grade, x, y, halfLocked: config.IsHalfLocked);
        }

        private void SpawnFromSave(CellSaveData data, int x, int y)
        {
            if (data.IsLocked)
            {
                LockedCell lc = SpawnLockedCell(x, y);
                if (!string.IsNullOrEmpty(data.CustomData))
                    lc.OnAfterLoad(data.CustomData);
                return;
            }

            if (string.IsNullOrEmpty(data.TypeId)) return;

            MergeItemData itemData = database.GetItem(data.TypeId);
            if (itemData == null)
            {
                Debug.LogWarning($"MergeGrid: unknown typeId '{data.TypeId}' at ({x},{y})");
                return;
            }

            MergeFieldObject obj = SpawnObject(itemData, data.Grade, x, y, halfLocked: data.IsHalfLocked);
            if (!string.IsNullOrEmpty(data.CustomData))
                obj.OnAfterLoad(data.CustomData);
        }

        private LockedCell SpawnLockedCell(int x, int y)
        {
            GameObject go = Instantiate(lockedCellPrefab, itemsContainer);
            PlaceInCell(go.GetComponent<RectTransform>(), x, y);
            LockedCell lc = go.GetComponent<LockedCell>();
            cells[x, y].Occupant = lc;
            return lc;
        }

        // startVisible=false lets flight-spawn callers (spawner, reward card) hide the item's
        // visual before OnFieldChanged fires — otherwise ClientOrderHighlightController would
        // light up the destination cell instantly, before the flying icon actually lands there.
        // halfLocked must be applied here, before OnFieldChanged fires — setting it after
        // SpawnObject returns leaves a window where ClientOrderTask binds the item as a free,
        // eligible instance (FindUnbound checks IsHalfLocked at bind time).
        public MergeFieldObject SpawnObject(MergeItemData data, int grade, int x, int y, bool startVisible = true, bool halfLocked = false)
        {
            GameObject go = Instantiate(data.Prefab, itemsContainer);
            PlaceInCell(go.GetComponent<RectTransform>(), x, y);
            MergeFieldObject obj = go.GetComponent<MergeFieldObject>();
            obj.Init(data, grade);
            if (!startVisible)
                obj.SetVisualVisible(false);
            if (halfLocked)
                obj.SetHalfLocked(true);
            cells[x, y].Occupant = obj;
            OnFieldChanged?.Invoke();
            return obj;
        }

        public void PlaceObject(MergeFieldObject obj, int x, int y)
        {
            cells[x, y].Occupant = obj;
            ((RectTransform)obj.transform).anchoredPosition = GetAnchoredPosition(x, y);
        }

        private void PlaceInCell(RectTransform rectTransform, int x, int y)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = cellSize;
            rectTransform.anchoredPosition = GetAnchoredPosition(x, y);
        }

        // For callers that clear an Occupant slot directly (e.g. MergeController.DeleteItem,
        // which nulls the cell synchronously but destroys the GameObject after a delayed
        // animation) and still need field-aware tasks to re-check presence right away.
        public void RaiseFieldChanged() => OnFieldChanged?.Invoke();

        public void RemoveObject(int x, int y)
        {
            MergeFieldObject obj = cells[x, y]?.Occupant;
            if (obj == null) return;
            cells[x, y].Occupant = null;
            Destroy(obj.gameObject);
            OnFieldChanged?.Invoke();
        }

        // Used by ClientOrderTask to detect when a bound instance has left the grid (merged away,
        // deleted, picked up) — checked against live Occupant slots rather than obj == null,
        // since Unity defers actual GameObject destruction to end of frame and Occupant slots are
        // cleared synchronously well before that (see ExecuteMerge/DeleteItem).
        public bool Contains(MergeFieldObject obj)
        {
            if (obj == null) return false;

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (cells[x, y].Occupant == obj) return true;

            return false;
        }

        // Used by ClientOrderTask to bind required items to live field instances without
        // double-matching an instance already bound to a different requirement slot.
        public MergeFieldObject FindUnbound(string typeId, int grade, HashSet<MergeFieldObject> excluded)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    MergeFieldObject occupant = cells[x, y].Occupant;
                    if (occupant == null || occupant.IsHalfLocked || excluded.Contains(occupant)) continue;
                    if (occupant.TypeId == typeId && occupant.Grade == grade) return occupant;
                }
            }
            return null;
        }

        // Used by UIInfoWindow to pick between the open/faded/mystery grade display: an instance
        // sitting on the board unlocked counts as "open" even if it never went through a tracked
        // merge (e.g. placed by level config or a spawner), while a locked one shows "faded"
        // instead of a full mystery placeholder. LockedCell occupants never match here — they
        // never call MergeFieldObject.Init, so their inherited TypeId stays unset.
        public bool TryGetInstanceState(string typeId, int grade, out bool isLocked)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    MergeFieldObject occupant = cells[x, y].Occupant;
                    if (occupant != null && occupant.TypeId == typeId && occupant.Grade == grade)
                    {
                        isLocked = occupant.IsHalfLocked;
                        return true;
                    }
                }
            isLocked = false;
            return false;
        }

        // Simple top-left scan — used for placing reward items where there's no anchor position
        // to search outward from (unlike spawner activation, which searches near the spawner).
        public MergeCell FindAnyEmpty()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (cells[x, y].IsEmpty) return cells[x, y];
            return null;
        }

        // Converts a world-space position (e.g. from a UI element outside the grid's hierarchy,
        // such as a task card) into the flying icon's anchored-position space.
        public Vector2 WorldToIconAnchoredPosition(Vector3 worldPos)
        {
            RectTransform parent = (RectTransform)flyingIconParent;
            Vector2 local = parent.InverseTransformPoint(worldPos);
            // Flying icons anchor to the container's top-left (see SpawnFlyingIconPool), while
            // InverseTransformPoint is relative to its pivot — shift between the two spaces.
            Rect rect = parent.rect;
            return local - new Vector2(rect.xMin, rect.yMax);
        }

        public MergeCell GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
            return cells[x, y];
        }

        public MergeCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

        public MergeCellBackground GetBackground(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
            return backgrounds[x, y];
        }

        public MergeCellBackground GetBackground(Vector2Int pos) => GetBackground(pos.x, pos.y);

        // Mirrors GridLayoutGroup's Upper-Left/Horizontal layout; grid-y grows upward,
        // so it's mapped to row = Height-1-y (UI rows grow downward).
        public Vector2 GetAnchoredPosition(int x, int y)
        {
            int row = Height - 1 - y;
            float posX = paddingTopLeft.x + x * (cellSize.x + spacing.x) + cellSize.x * 0.5f;
            float posY = -(paddingTopLeft.y + row * (cellSize.y + spacing.y) + cellSize.y * 0.5f);
            return new Vector2(posX, posY);
        }

        public Vector2 GetAnchoredPosition(Vector2Int pos) => GetAnchoredPosition(pos.x, pos.y);

        public bool IsValidPosition(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public bool IsValidPosition(Vector2Int pos) => IsValidPosition(pos.x, pos.y);

        public List<MergeCell> GetNeighbors(int x, int y)
        {
            var result = new List<MergeCell>(4);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (Vector2Int dir in dirs)
            {
                MergeCell cell = GetCell(x + dir.x, y + dir.y);
                if (cell != null) result.Add(cell);
            }
            return result;
        }

        public List<MergeCell> GetNeighbors8(int x, int y)
        {
            var result = new List<MergeCell>(8);
            Vector2Int[] dirs =
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1)
            };
            foreach (Vector2Int dir in dirs)
            {
                MergeCell cell = GetCell(x + dir.x, y + dir.y);
                if (cell != null) result.Add(cell);
            }
            return result;
        }

        // BFS — nearest empty cell from (x, y)
        public MergeCell FindNearestEmpty(int x, int y)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));

            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();
                if (!visited.Add(pos)) continue;

                MergeCell cell = GetCell(pos);
                if (cell != null && cell.IsEmpty) return cell;

                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int next = pos + dir;
                    if (IsValidPosition(next) && !visited.Contains(next))
                        queue.Enqueue(next);
                }
            }

            return null;
        }
    }
}
