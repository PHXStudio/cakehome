using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    // Same type/grade picker as OrderItemPropertyDrawer (Tasks/Editor/OrderItemPropertyDrawer.cs)
    // — SpawnerQueueReward has the identical typeId/grade shape, reusing the same MergeTypePickerWindow.
    [CustomPropertyDrawer(typeof(SpawnerQueueReward))]
    public class SpawnerQueueRewardPropertyDrawer : PropertyDrawer
    {
        private const float PREVIEW_SIZE = 40f;

        private static MergeDatabase cachedDatabase;
        private static Dictionary<string, EntryCache> entryCache = new Dictionary<string, EntryCache>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty typeIdProp = property.FindPropertyRelative("spawnerTypeId");
            SerializedProperty gradeProp = property.FindPropertyRelative("spawnerGrade");
            SerializedProperty spawnFloatingImageProp = property.FindPropertyRelative("spawnFloatingImage");

            EntryCache cache = GetEntryCache(property.propertyPath, typeIdProp.stringValue, gradeProp.intValue);

            // SimpleRewardDrawer reserves its header row (type dropdown + reset button) by adding
            // it on top of our GetPropertyHeight, but still passes the full rect starting at the
            // header's y — by design, so a reward drawer can turn the header into a foldout if it
            // wants to. We don't need that, so just skip past the reserved header space here.
            float ownHeight = GetPropertyHeight(property, label);
            float headerReserved = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (position.height >= ownHeight + headerReserved - 0.1f)
            {
                position = new Rect(position.x, position.y + headerReserved, position.width, ownHeight);
            }

            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            Rect previewRect = new Rect(contentRect.x, contentRect.y, PREVIEW_SIZE, Mathf.Min(PREVIEW_SIZE, contentRect.height));
            DrawPreview(previewRect, cache.GradeData);

            Rect buttonsRect = new Rect(contentRect.x + PREVIEW_SIZE + 4, contentRect.y, contentRect.width - PREVIEW_SIZE - 4, contentRect.height);

            Rect typeButtonRect = new Rect(buttonsRect.x, buttonsRect.y, buttonsRect.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(typeButtonRect, cache.TypeButtonText))
            {
                MergeTypePickerWindow.PickType(typeIdProp, gradeProp);
            }

            Rect gradeButtonRect = new Rect(buttonsRect.x, typeButtonRect.y + EditorGUIUtility.singleLineHeight + 2, buttonsRect.width, EditorGUIUtility.singleLineHeight);
            using (new EditorGUI.DisabledScope(disabled: cache.ItemData == null))
            {
                if (GUI.Button(gradeButtonRect, cache.GradeButtonText))
                {
                    MergeTypePickerWindow.PickGrade(gradeProp, cache.ItemData);
                }
            }

            Rect floatingImageRect = new Rect(buttonsRect.x, gradeButtonRect.y + EditorGUIUtility.singleLineHeight + 2, buttonsRect.width, EditorGUIUtility.singleLineHeight);
            spawnFloatingImageProp.boolValue = EditorGUI.ToggleLeft(floatingImageRect, "Spawn Floating Image", spawnFloatingImageProp.boolValue);

            EditorGUI.EndProperty();
        }

        private static EntryCache GetEntryCache(string propertyPath, string typeId, int grade)
        {
            if (!entryCache.TryGetValue(propertyPath, out EntryCache cache) || cache.TypeId != typeId || cache.Grade != grade)
            {
                cache = new EntryCache(typeId, grade);
                entryCache[propertyPath] = cache;
            }

            return cache;
        }

        private static MergeDatabase GetDatabase()
        {
            if (cachedDatabase == null)
                cachedDatabase = EditorUtils.GetAsset<MergeDatabase>();

            return cachedDatabase;
        }

        private static string GetTypeButtonText(string typeId, MergeItemData itemData)
        {
            if (string.IsNullOrEmpty(typeId))
                return "<None>";

            if (itemData == null)
                return $"Missing: {typeId}";

            string displayName = itemData.GetGradeData(1)?.DisplayName;

            return string.IsNullOrEmpty(displayName) ? typeId : $"{displayName} ({typeId})";
        }

        private static string GetGradeButtonText(int grade, MergeGradeData gradeData)
        {
            string displayName = gradeData?.DisplayName;

            return string.IsNullOrEmpty(displayName) ? $"Grade {grade}" : $"Grade {grade} - {displayName}";
        }

        private static void DrawPreview(Rect rect, MergeGradeData gradeData)
        {
            GUI.Box(rect, GUIContent.none);

            Texture2D previewTexture = gradeData?.Sprite != null ? AssetPreview.GetAssetPreview(gradeData.Sprite) : null;
            if (previewTexture != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), previewTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), EditorCustomStyles.GetMissingIcon());
            }
        }

        private static MergeItemData FindItemData(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                Debug.Log("[SpawnerQueueRewardPropertyDrawer] FindItemData: typeId is null/empty.");
                return null;
            }

            MergeDatabase database = GetDatabase();
            if (database == null)
            {
                Debug.Log("[SpawnerQueueRewardPropertyDrawer] FindItemData: database is null (EditorUtils.GetAsset<MergeDatabase>() found nothing).");
                return null;
            }

            foreach (MergeItemData item in database.Items)
            {
                if (item.TypeId == typeId)
                    return item;
            }

            Debug.Log($"[SpawnerQueueRewardPropertyDrawer] FindItemData: no item with TypeId '{typeId}' found among {database.Items.Count} items. Available: {string.Join(", ", database.Items.Select(i => i.TypeId))}");
            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return Mathf.Max(EditorGUIUtility.singleLineHeight * 3 + 4, PREVIEW_SIZE);
        }

        private class EntryCache
        {
            public string TypeId { get; }
            public int Grade { get; }
            public MergeItemData ItemData { get; }
            public MergeGradeData GradeData { get; }
            public string TypeButtonText { get; }
            public string GradeButtonText { get; }

            public EntryCache(string typeId, int grade)
            {
                TypeId = typeId;
                Grade = grade;
                ItemData = FindItemData(typeId);
                GradeData = ItemData?.GetGradeData(grade);
                TypeButtonText = GetTypeButtonText(typeId, ItemData);
                GradeButtonText = GetGradeButtonText(grade, GradeData);
            }
        }
    }
}
