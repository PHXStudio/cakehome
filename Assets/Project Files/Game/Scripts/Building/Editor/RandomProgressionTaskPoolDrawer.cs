using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    // Renders each entry's weight as a 0-100% "Chance" slider instead of a raw float. Percent is
    // always weight / totalWeight * 100, so dragging one entry automatically shifts every other
    // entry's displayed percent — solving for this entry's new weight from the target percent
    // (holding every other entry's weight fixed) is what makes that fall out for free.
    //
    // Each entry is boxed so adjacent entries don't visually blend together. The task's own
    // "character" field is skipped entirely — a random-pool task always gets its character from
    // RandomProgressionTaskPool.characters (see ClientOrderTaskDefinition.CreateTask), so the
    // field is dead here. The task object itself is flattened (no "Task" foldout to expand) —
    // its items/coinsReward fields are drawn directly, one indent level in, as if already open.
    [CustomPropertyDrawer(typeof(RandomProgressionTaskPool))]
    public class RandomProgressionTaskPoolDrawer : PropertyDrawer
    {
        const float REMOVE_BUTTON_WIDTH = 20f;
        const float ADD_BUTTON_WIDTH    = 20f;
        const float INDENT              = 12f;
        const float ENTRY_INDENT        = 12f; // extra indent for entry boxes, on top of INDENT — reads as children of "Weighted Tasks"
        const float BOX_PADDING         = 6f;
        const float ENTRY_SPACING       = 6f;
        const float NARROW_VIEW_WIDTH   = 230f; // below this inspector width, the Chance slider row wraps onto 2 lines

        // GetPropertyHeight has no Rect to measure, so both it and OnGUI key off
        // currentViewWidth instead — the one width signal available in both places.
        private static bool IsNarrow() => EditorGUIUtility.currentViewWidth < NARROW_VIEW_WIDTH;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty tasksProp                     = property.FindPropertyRelative("tasks");
            SerializedProperty charactersProp                 = property.FindPropertyRelative("characters");
            SerializedProperty stopSpawnCoinMultiplierProp    = property.FindPropertyRelative("stopSpawnCoinMultiplier");
            SerializedProperty maxSimultaneousRandomTasksProp = property.FindPropertyRelative("maxSimultaneousRandomTasks");

            Rect foldoutRect = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (!property.isExpanded) return;

            float y = foldoutRect.yMax + vpad;

            float charactersHeight = EditorGUI.GetPropertyHeight(charactersProp, true);
            Rect charactersRect = new Rect(position.x + INDENT, y, position.width - INDENT, charactersHeight);
            EditorGUI.PropertyField(charactersRect, charactersProp, true);
            y += charactersHeight + vpad;

            Rect stopSpawnRect = new Rect(position.x + INDENT, y, position.width - INDENT, line);
            EditorGUI.PropertyField(stopSpawnRect, stopSpawnCoinMultiplierProp, new GUIContent("Stop Spawn Coin Multiplier (0 = disabled)"));
            y += line + vpad;

            Rect maxSimultaneousRect = new Rect(position.x + INDENT, y, position.width - INDENT, line);
            EditorGUI.PropertyField(maxSimultaneousRect, maxSimultaneousRandomTasksProp, new GUIContent("Max Simultaneous Random Tasks"));
            y += line + vpad;

            float totalWeight = 0f;
            for (int i = 0; i < tasksProp.arraySize; i++)
                totalWeight += tasksProp.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue;

            Rect headerRect = new Rect(position.x + INDENT, y, position.width - INDENT - ADD_BUTTON_WIDTH - 4f, line);
            EditorGUI.LabelField(headerRect, "Weighted Tasks", EditorStyles.boldLabel);

            Rect addRect = new Rect(headerRect.xMax + 4f, y, ADD_BUTTON_WIDTH, line);
            if (GUI.Button(addRect, "+"))
            {
                tasksProp.arraySize++;
                tasksProp.GetArrayElementAtIndex(tasksProp.arraySize - 1).FindPropertyRelative("weight").floatValue = 1f;
            }
            y += line + vpad;

            int removeIndex = -1;
            float rowX     = position.x + INDENT + ENTRY_INDENT;
            float rowWidth = position.width - INDENT - ENTRY_INDENT;

            for (int i = 0; i < tasksProp.arraySize; i++)
            {
                SerializedProperty entry = tasksProp.GetArrayElementAtIndex(i);

                float boxHeight = GetEntryContentHeight(entry) + BOX_PADDING * 2f;
                Rect boxRect = new Rect(rowX, y, rowWidth, boxHeight);
                GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

                DrawEntry(entry, boxRect, totalWeight, out bool removeRequested);
                if (removeRequested) removeIndex = i;

                y += boxHeight + ENTRY_SPACING;
            }

            if (removeIndex >= 0)
                tasksProp.DeleteArrayElementAtIndex(removeIndex);
        }

        private void DrawEntry(SerializedProperty entry, Rect boxRect, float totalWeight, out bool removeRequested)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty weightProp = entry.FindPropertyRelative("weight");
            SerializedProperty taskProp   = entry.FindPropertyRelative("task");
            SerializedProperty itemsProp  = taskProp.FindPropertyRelative("items");
            SerializedProperty coinsProp  = taskProp.FindPropertyRelative("coinsReward");

            float innerX     = boxRect.x + BOX_PADDING;
            float innerWidth = boxRect.width - BOX_PADDING * 2f;
            float y          = boxRect.y + BOX_PADDING;

            // Task flattened in place — no "Task" foldout to expand, and its character field is
            // skipped (always overridden by this pool's own character roll at spawn time).
            float itemsHeight = EditorGUI.GetPropertyHeight(itemsProp, true);
            Rect itemsRect = new Rect(innerX, y, innerWidth, itemsHeight);
            EditorGUI.PropertyField(itemsRect, itemsProp, true);
            y += itemsHeight + vpad;

            float coinsHeight = EditorGUI.GetPropertyHeight(coinsProp, true);
            Rect coinsRect = new Rect(innerX, y, innerWidth, coinsHeight);
            EditorGUI.PropertyField(coinsRect, coinsProp, true);
            y += coinsHeight + vpad;

            float weight  = weightProp.floatValue;
            float percent = totalWeight > 0f ? weight / totalWeight * 100f : 0f;

            // Rounded to tenths for display only — the Slider's own numeric field shows this
            // value directly, so a separate "%" label isn't needed anymore.
            float displayPercent = Mathf.Round(percent * 10f) / 10f;

            Rect sliderRect, removeRect;

            if (IsNarrow())
            {
                sliderRect = new Rect(innerX, y, innerWidth, line);
                y += line + vpad;
                removeRect = new Rect(innerX + innerWidth - REMOVE_BUTTON_WIDTH, y, REMOVE_BUTTON_WIDTH, line);
            }
            else
            {
                sliderRect = new Rect(innerX, y, innerWidth - REMOVE_BUTTON_WIDTH - 4f, line);
                removeRect = new Rect(sliderRect.xMax + 4f, y, REMOVE_BUTTON_WIDTH, line);
            }

            EditorGUI.BeginChangeCheck();
            float newPercent = EditorGUI.Slider(sliderRect, "Chance", displayPercent, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                float othersWeight = totalWeight - weight;
                if (othersWeight <= 0f)
                {
                    weightProp.floatValue = Mathf.Max(0.01f, weight);
                }
                else
                {
                    float clampedPercent = Mathf.Clamp(newPercent, 0f, 99.9f);
                    weightProp.floatValue = Mathf.Max(0f, othersWeight * clampedPercent / (100f - clampedPercent));
                }
            }

            removeRequested = GUI.Button(removeRect, "x");
        }

        private float GetEntryContentHeight(SerializedProperty entry)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty taskProp  = entry.FindPropertyRelative("task");
            SerializedProperty itemsProp = taskProp.FindPropertyRelative("items");
            SerializedProperty coinsProp = taskProp.FindPropertyRelative("coinsReward");

            float height = line; // weight slider row
            if (IsNarrow()) height += vpad + line; // percent+remove wraps onto its own line
            height += vpad + EditorGUI.GetPropertyHeight(itemsProp, true);
            height += vpad + EditorGUI.GetPropertyHeight(coinsProp, true);

            return height;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;
            float height = line; // foldout

            if (!property.isExpanded) return height;

            SerializedProperty tasksProp      = property.FindPropertyRelative("tasks");
            SerializedProperty charactersProp = property.FindPropertyRelative("characters");

            height += vpad + EditorGUI.GetPropertyHeight(charactersProp, true);
            height += vpad + line; // stop-spawn coin multiplier row
            height += vpad + line; // max simultaneous random tasks row
            height += vpad + line; // "Weighted Tasks" header row

            for (int i = 0; i < tasksProp.arraySize; i++)
            {
                SerializedProperty entry = tasksProp.GetArrayElementAtIndex(i);
                height += ENTRY_SPACING + GetEntryContentHeight(entry) + BOX_PADDING * 2f;
            }

            return height;
        }
    }
}
