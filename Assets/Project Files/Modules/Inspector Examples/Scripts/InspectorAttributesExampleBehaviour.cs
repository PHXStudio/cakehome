#pragma warning disable 0649, 0414

using System;
using UnityEngine;

namespace Watermelon
{
    [HideScriptField]
    [HelpButton("Inspector Docs", "https://watermelon-games.com")]
    public class InspectorAttributesExampleBehaviour : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // TAB: Fields — sliders, toggles, misc property drawers
        // ═══════════════════════════════════════════════════════════

        [Tab("Fields")]
        [LineSpacer("Sliders")]
        [Slider(0f, 10f)]
        [SerializeField] float sliderFloat = 5f;

        [Tab("Fields")]
        [Slider(0, 100)]
        [SerializeField] int sliderInt = 50;

        [Tab("Fields")]
        [MinMaxSlider(0f, 1f)]
        [SerializeField] DuoFloat minMaxRange = new DuoFloat(0f, 1f);

        [Tab("Fields")]
        [LineSpacer("Toggles")]
        [OnOff]
        [SerializeField] bool onOffBool;

        [Tab("Fields")]
        [OnOff(true)]
        [SerializeField] bool onOffWideBool;

        [Tab("Fields")]
        [Toggle]
        [SerializeField] bool toggleBool;

        [Tab("Fields")]
        [LineSpacer("Other")]
        [EnumFlags]
        [SerializeField] ExampleFlags flagsField;

        [Tab("Fields")]
        [ReorderableList]
        [SerializeField] string[] reorderableList;

        [Tab("Fields")]
        [UniqueID]
        [SerializeField] string uniqueId;

        [Tab("Fields")]
        [DrawReference]
        [SerializeField] ScriptableObject drawReference;

        // ═══════════════════════════════════════════════════════════
        // TAB: Groups — box, foldout, horizontal, unpack
        // ═══════════════════════════════════════════════════════════

        [Tab("Groups")]
        [BoxGroup("Box", "Box Group")]
        [SerializeField] int boxA;

        [Tab("Groups")]
        [BoxGroup("Box")]
        [SerializeField] int boxB;

        [Tab("Groups")]
        [BoxGroup("Box")]
        [SerializeField] int boxC;

        [Tab("Groups")]
        [BoxFoldout("BF", "Box Foldout", defaultState: true)]
        [SerializeField] string bfA;

        [Tab("Groups")]
        [BoxFoldout("BF")]
        [SerializeField] string bfB;

        [Tab("Groups")]
        [Foldout("Fold", "Simple Foldout")]
        [SerializeField] float foldA;

        [Tab("Groups")]
        [Foldout("Fold")]
        [SerializeField] float foldB;

        [Tab("Groups")]
        [HorizontalGroup("HG")]
        [SerializeField] float hgA;

        [Tab("Groups")]
        [HorizontalGroup("HG")]
        [SerializeField] float hgB;

        [Tab("Groups")]
        [HorizontalGroup("HG")]
        [SerializeField] float hgC;

        [Tab("Groups")]
        [LineSpacer("Unpack Nested")]
        [UnpackNested]
        [SerializeField] NestedData nested;

        // ═══════════════════════════════════════════════════════════
        // TAB: Conditions — visibility and editable state
        // ═══════════════════════════════════════════════════════════

        [Tab("Conditions")]
        [SerializeField] bool condition;

        [Tab("Conditions")]
        [LineSpacer("Visibility")]
        [ShowIf("condition")]
        [SerializeField] float shownIfTrue;

        [Tab("Conditions")]
        [HideIf("condition")]
        [SerializeField] float hiddenIfTrue;

        [Tab("Conditions")]
        [Hide]
        [SerializeField] float alwaysHidden;

        [Tab("Conditions")]
        [LineSpacer("States")]
        [ReadOnly]
        [SerializeField] string readOnly = "read-only value";

        [Tab("Conditions")]
        [EnableIf("condition")]
        [SerializeField] float enabledIfTrue;

        [Tab("Conditions")]
        [DisableIf("condition")]
        [SerializeField] float disabledIfTrue;

        // ═══════════════════════════════════════════════════════════
        // TAB: UI — info boxes, labels, layout, events
        // ═══════════════════════════════════════════════════════════

        [Tab("UI")]
        [LineSpacer("Info Boxes")]
        [InfoBox("Normal info box")]
        [SerializeField] float infoNormal;

        [Tab("UI")]
        [InfoBox("Warning info box", InfoBoxType.Warning)]
        [SerializeField] float infoWarning;

        [Tab("UI")]
        [InfoBox("Error info box", InfoBoxType.Error)]
        [SerializeField] float infoError;

        [Tab("UI")]
        [LineSpacer("Labels & Layout")]
        [Label("Custom Label Text")]
        [SerializeField] int labeledField;

        [Tab("UI")]
        [LabelWidth(200f)]
        [SerializeField] int wideLabelField;

        [Tab("UI")]
        [Indent]
        [SerializeField] int indented;

        [Tab("UI")]
        [Indent(48f)]
        [SerializeField] int deepIndented;

        [Tab("UI")]
        [LineSpacer("Inline Button & Callback")]
        [InlineButton("Log", "LogValue")]
        [OnValueChanged("OnValueChange")]
        [SerializeField] int trackedValue;

        [Tab("UI")]
        [ShowNonSerialized]
        private int nonSerializedField = 99;

        // ═══════════════════════════════════════════════════════════
        // Buttons — rendered outside tabs
        // ═══════════════════════════════════════════════════════════

        [Button]
        private void SimpleButton() => Debug.Log("Simple button clicked");

        [Button("Custom Button Label")]
        private void LabeledButton() => Debug.Log("Labeled button clicked");

        // ═══════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════

        private void LogValue() => Debug.Log($"Inline: {trackedValue}");
        private void OnValueChange() => Debug.Log($"Changed: {trackedValue}");

        [Serializable]
        public struct NestedData
        {
            public int intValue;
            public string stringValue;
            public Vector3 position;
        }
    }

    [Flags]
    public enum ExampleFlags
    {
        None  = 0,
        Alpha = 1 << 0,
        Beta  = 1 << 1,
        Gamma = 1 << 2,
        Delta = 1 << 3
    }
}
