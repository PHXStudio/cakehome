using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Merge Level", menuName = "Game/Merge Level")]
    public class MergeLevelData : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] int gridWidth = 7;
        [SerializeField, LevelEditorSetting] int gridHeight = 9;
        [SerializeField, LevelEditorSetting] CellConfig[] cells;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public CellConfig GetCell(int x, int y) => cells[y * gridWidth + x];

#if UNITY_EDITOR
        private void OnValidate()
        {
            int required = gridWidth * gridHeight;
            if (cells == null || cells.Length != required)
                System.Array.Resize(ref cells, required);
        }
#endif
    }
}
