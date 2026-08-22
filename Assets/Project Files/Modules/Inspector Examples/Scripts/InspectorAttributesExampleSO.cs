#pragma warning disable 0649, 0414

using UnityEngine;

namespace Watermelon
{
    [HideScriptField]
    [HelpButton("Inspector Docs", "https://watermelon-games.com")]
    [CreateAssetMenu(menuName = "Data/Core/Examples/Inspector Attributes", fileName = "Inspector Attributes Example")]
    public class InspectorAttributesExampleSO : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════
        // BoxGroup — flat box with header label
        // ═══════════════════════════════════════════════════════════

        [BoxGroup("Basic", "Basic Fields")]
        [SerializeField] string textField = "example";

        [BoxGroup("Basic")]
        [SerializeField] int intField = 42;

        [BoxGroup("Basic")]
        [ReadOnly]
        [SerializeField] float readOnly = 3.14f;

        [BoxGroup("Basic")]
        [InfoBox("Fields above are read-only at runtime.", InfoBoxType.Normal)]
        [Slider(0f, 1f)]
        [SerializeField] float sliderField = 0.5f;

        [BoxGroup("Basic")]
        [MinMaxSlider(0f, 100f)]
        [SerializeField] DuoFloat rangeField = new DuoFloat(10f, 90f);

        // ═══════════════════════════════════════════════════════════
        // BoxFoldout — collapsible box
        // ═══════════════════════════════════════════════════════════

        [BoxFoldout("BF", "Box Foldout Settings", defaultState: false)]
        [InfoBox("This section is collapsed by default.", InfoBoxType.Warning)]
        [SerializeField] float bfA;

        [BoxFoldout("BF")]
        [SerializeField] float bfB;

        [BoxFoldout("BF")]
        [OnOff]
        [SerializeField] bool bfToggle;

        // ═══════════════════════════════════════════════════════════
        // Foldout — simple foldout group (no box)
        // ═══════════════════════════════════════════════════════════

        [Foldout("Fold", "Simple Foldout")]
        [SerializeField] int foldA;

        [Foldout("Fold")]
        [EnumFlags]
        [SerializeField] ExampleFlags foldFlags;

        [Foldout("Fold")]
        [ReorderableList]
        [SerializeField] string[] foldList;

        // ═══════════════════════════════════════════════════════════
        // Conditions — visibility and state
        // ═══════════════════════════════════════════════════════════

        [LineSpacer("Conditions")]
        [SerializeField] bool conditionBool;

        [ShowIf("conditionBool")]
        [SerializeField] float shownIfTrue;

        [HideIf("conditionBool")]
        [SerializeField] float hiddenIfTrue;

        [EnableIf("conditionBool")]
        [SerializeField] float enabledIfTrue;

        [DisableIf("conditionBool")]
        [SerializeField] float disabledIfTrue;

        // ═══════════════════════════════════════════════════════════
        // Layout helpers
        // ═══════════════════════════════════════════════════════════

        [LineSpacer("Layout Helpers")]
        [Label("Custom Field Label")]
        [SerializeField] int labeledField;

        [LabelWidth(220f)]
        [SerializeField] int veryLongFieldNameHere;

        [Indent]
        [SerializeField] int indented;

        [HorizontalGroup("HG")]
        [SerializeField] float hgLeft;

        [HorizontalGroup("HG")]
        [SerializeField] float hgRight;

        // ═══════════════════════════════════════════════════════════
        // ScriptableObject creation — creates asset when null
        // ═══════════════════════════════════════════════════════════

        [LineSpacer("Reference Fields")]
        [DrawReference]
        [SerializeField] ScriptableObject drawReference;

        [CreateScriptableObject]
        [SerializeField] InspectorAttributesExampleSO nestedSO;

        // ═══════════════════════════════════════════════════════════
        // Unique ID
        // ═══════════════════════════════════════════════════════════

        [LineSpacer("Unique ID")]
        [UniqueID]
        [SerializeField] string uniqueId;

        // ═══════════════════════════════════════════════════════════
        // Buttons
        // ═══════════════════════════════════════════════════════════

        [Button]
        private void PrintData() => Debug.Log($"textField={textField}, intField={intField}");

        [Button("Reset to Defaults")]
        private void ResetDefaults()
        {
            textField = "example";
            intField = 42;
            readOnly = 3.14f;
        }
    }
}
