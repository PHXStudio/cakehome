using UnityEditorInternal;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "ModuleDefine", menuName = "Data/Core/Module Define Settings")]
    public class ModuleDefineSettings : ScriptableObject
    {
        [SerializeField] private string define;
        public string Define => define;

        [SerializeField] private string detectionType;
        public string DetectionType => detectionType;

        [SerializeField] private AssemblyDefinitionAsset moduleAsmdef;
        public AssemblyDefinitionAsset ModuleAsmdef => moduleAsmdef;

        // Alternative to moduleAsmdef for asmdefs that only exist on disk while a 3rd-party
        // package is installed (e.g. Unity.Purchasing). A serialized object reference to such an
        // asmdef turns into a broken "Missing" reference the moment the package is removed. Auto-link
        // resolves the target by filename at cache-rebuild time instead, so nothing is ever serialized
        // that can go missing.
        [SerializeField] private bool autoLinkAsmdef;
        public bool AutoLinkAsmdef => autoLinkAsmdef;

        [SerializeField] private string asmdefName;
        public string AsmdefName => asmdefName;

        // Optional file/DLL path to monitor for deletion (e.g. "GoogleMobileAds.Unity.dll").
        // When the file appears in deletedAssets the define is proactively disabled before
        // the next domain reload — same behaviour as the legacy RegisteredDefine.FilePath.
        [SerializeField] private string filePath;
        public string FilePath => filePath;

        // Defines whose asmdefs should be referenced by this module when active
        [SerializeField] private string[] optionalDependencies;
        public string[] OptionalDependencies => optionalDependencies;
    }
}
