using System.IO;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Core Settings", menuName = "Data/Core/Core Settings")]
    public class CoreSettings : ScriptableObject
    {
        [Header("Path")]
        [SerializeField] string dataFolder = Path.Combine("Assets", "Project Files", "Data");
        public string DataFolder => dataFolder;

        [SerializeField] string scenesFolder = Path.Combine("Assets", "Project Files", "Game", "Scenes");
        public string ScenesFolder => scenesFolder;

        [Header("Init")]
        [SerializeField] string initSceneName = "Init";
        public string InitSceneName => initSceneName;

        [SerializeField] bool autoLoadInitializer = true;
        public bool AutoLoadInitializer => autoLoadInitializer;

        [Header("Editor")]
        [SerializeField] bool useCustomInspector = true;
        public bool UseCustomInspector => useCustomInspector;

        [SerializeField] bool useHierarchyIcons = true;
        public bool UseHierarchyIcons => useHierarchyIcons;

        [Header("Other")]
        [SerializeField] bool showWatermelonPromotions = true;
        public bool ShowWatermelonPromotions => showWatermelonPromotions;
    }
}
