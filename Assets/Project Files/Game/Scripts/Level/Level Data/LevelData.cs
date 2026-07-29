#pragma warning disable 0649

using UnityEngine;

namespace Watermelon
{

    [System.Serializable]
    public class LevelData : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] Layer[] layers;
        public int AmountOfLayers => layers.Length;

        [SerializeField, LevelEditorSetting] int bottomLayerWidth = 10;
        public int BottomLayerWidth => bottomLayerWidth;

        [SerializeField, LevelEditorSetting] int bottomLayerHeight = 10;
        public int BottomLayerHeight => bottomLayerHeight;

        [SerializeField, LevelEditorSetting] bool useInRandomizer;
        public bool UseInRandomizer => useInRandomizer;

        [SerializeField, LevelEditorSetting] int elementsPerLevel = 8;
        public int ElementsPerLevel => elementsPerLevel;

        [SerializeField, LevelEditorSetting] int coinsReward = 20;
        public int CoinsReward => coinsReward;

        [SerializeField, LevelEditorSetting] string editorNote; // used only in level editor

        [SerializeField] IntToggle timer = new IntToggle(false, 60);
        public IntToggle Timer => timer;

        public int SetsAmount => (GetAmountOfFilledCells() - (GetAmountOfFilledCells() % 3)) / 3;
        public float Difficulty => Mathf.Round(Mathf.Clamp(SetsAmount / (float)elementsPerLevel, 1, float.MaxValue) * 10.0f) * 0.1f;

        public Layer GetLayer(int i)
        {
            if (i < AmountOfLayers && i >= 0) return layers[i];

            return null;
        }

        public int GetAmountOfFilledCells()
        {
            int counter = 0;

            for (int i = 0; i < AmountOfLayers; i++)
            {
                counter += layers[i].GetAmountOfFilledCells();
            }

            return counter;
        }
    }
}