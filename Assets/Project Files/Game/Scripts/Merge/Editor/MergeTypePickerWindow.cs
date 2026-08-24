using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Watermelon
{
    // Single floating window that handles both Type and Grade selection for SpawnEntry.
    // Switching from Type -> Grade after a pick just flips "mode" and repaints — it never
    // closes/recreates the window.
    //
    // A cached SerializedObject is NOT kept across GUI frames: ApplyModifiedProperties() on the
    // target (a prefab asset) can make Unity reimport/recreate its native serialized data shortly
    // after, which silently invalidates any SerializedObject created before that point. Instead we
    // keep only the plain target Object + property path, and build a fresh SerializedObject right
    // before every read/write.
    public class MergeTypePickerWindow : EditorWindow
    {
        private enum Mode
        {
            Type,
            Grade
        }

        private Mode mode;

        private Object targetObject;
        private string propertyPath;
        private string chainedGradePropertyPath;

        private MergeDatabase database;
        private MergeItemData gradeItemData;
        private string selectedTypeId;
        private int selectedGrade;

        private Vector2 scrollView;
        private GUIStyle boxStyle;

        private static MergeTypePickerWindow window;

        public static void PickType(SerializedProperty property, SerializedProperty gradeProperty = null)
        {
            OpenWindow();

            window.mode = Mode.Type;
            window.targetObject = property.serializedObject.targetObject;
            window.propertyPath = property.propertyPath;
            window.chainedGradePropertyPath = gradeProperty?.propertyPath;
            window.selectedTypeId = property.stringValue;
            window.database = EditorUtils.GetAsset<MergeDatabase>();
            window.titleContent = new GUIContent("Merge Type Picker");
            window.scrollView = Vector2.zero;

            window.Repaint();
        }

        public static void PickGrade(SerializedProperty property, MergeItemData itemData)
        {
            OpenWindow();

            window.mode = Mode.Grade;
            window.targetObject = property.serializedObject.targetObject;
            window.propertyPath = property.propertyPath;
            window.gradeItemData = itemData;
            window.selectedGrade = property.intValue;
            window.titleContent = new GUIContent("Merge Grade Picker");
            window.scrollView = Vector2.zero;

            window.Repaint();
        }

        private static void OpenWindow()
        {
            if (window == null)
            {
                window = GetWindow<MergeTypePickerWindow>(true);
            }

            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            boxStyle = new GUIStyle(EditorCustomStyles.Skin.box);
            boxStyle.overflow = new RectOffset(0, 0, 0, 0);
            boxStyle.margin = new RectOffset(4, 4, 5, 5);
        }

        private void OnGUI()
        {
            if (targetObject == null)
            {
                Close();

                return;
            }

            if (mode == Mode.Type)
            {
                DrawTypeList();
            }
            else
            {
                DrawGradeList();
            }
        }

        private void DrawTypeList()
        {
            if (database == null || database.Items.Count == 0)
            {
                EditorGUILayout.HelpBox("Merge database is empty or not found!", MessageType.Info);

                return;
            }

            EditorGUILayout.BeginVertical();

            scrollView = EditorGUILayout.BeginScrollView(scrollView);

            for (int i = 0; i < database.Items.Count; i++)
            {
                MergeItemData item = database.Items[i];
                Color defaultColor = GUI.backgroundColor;

                if (selectedTypeId == item.TypeId)
                {
                    GUI.backgroundColor = Color.yellow;
                }

                Rect elementRect = GUILayoutUtility.GetRect(1, float.MaxValue, 58, 58, boxStyle);

                if (GUI.Button(elementRect, GUIContent.none, boxStyle))
                {
                    SelectType(item.TypeId);

                    return;
                }

                using (new EditorGUI.DisabledScope(disabled: true))
                {
                    elementRect.x += 4;
                    elementRect.width -= 8;
                    elementRect.height -= 8;

                    DrawTypeElement(elementRect, item);
                }

                GUI.backgroundColor = defaultColor;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGradeList()
        {
            if (gradeItemData == null || gradeItemData.MaxGrade <= 0)
            {
                EditorGUILayout.HelpBox("No grades available for this type.", MessageType.Info);

                return;
            }

            EditorGUILayout.BeginVertical();

            scrollView = EditorGUILayout.BeginScrollView(scrollView);

            for (int grade = 1; grade <= gradeItemData.MaxGrade; grade++)
            {
                MergeGradeData gradeData = gradeItemData.GetGradeData(grade);
                Color defaultColor = GUI.backgroundColor;

                if (selectedGrade == grade)
                {
                    GUI.backgroundColor = Color.yellow;
                }

                Rect elementRect = GUILayoutUtility.GetRect(1, float.MaxValue, 58, 58, boxStyle);

                if (GUI.Button(elementRect, GUIContent.none, boxStyle))
                {
                    SelectGrade(grade);

                    return;
                }

                using (new EditorGUI.DisabledScope(disabled: true))
                {
                    elementRect.x += 4;
                    elementRect.width -= 8;
                    elementRect.height -= 8;

                    DrawGradeElement(elementRect, grade, gradeData);
                }

                GUI.backgroundColor = defaultColor;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void SelectType(string typeId)
        {
            SerializedObject so = new SerializedObject(targetObject);

            SerializedProperty property = so.FindProperty(propertyPath);
            property.stringValue = typeId;

            so.ApplyModifiedProperties();

            if (!string.IsNullOrEmpty(chainedGradePropertyPath))
            {
                MergeItemData item = null;
                foreach (MergeItemData candidate in database.Items)
                {
                    if (candidate.TypeId == typeId)
                    {
                        item = candidate;

                        break;
                    }
                }

                mode = Mode.Grade;
                propertyPath = chainedGradePropertyPath;
                gradeItemData = item;
                selectedGrade = so.FindProperty(chainedGradePropertyPath).intValue;
                titleContent = new GUIContent("Merge Grade Picker");
                scrollView = Vector2.zero;

                Repaint();
            }
            else
            {
                Close();
            }
        }

        private void SelectGrade(int grade)
        {
            SerializedObject so = new SerializedObject(targetObject);

            SerializedProperty property = so.FindProperty(propertyPath);
            property.intValue = grade;

            so.ApplyModifiedProperties();

            Close();
        }

        private void DrawTypeElement(Rect rect, MergeItemData item)
        {
            float defaultYPosition = rect.y;

            rect.width -= 60;

            MergeGradeData gradeData = item.GetGradeData(1);
            string displayName = gradeData?.DisplayName;

            Rect propertyPosition = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(propertyPosition, string.IsNullOrEmpty(displayName) ? item.TypeId : displayName);

            propertyPosition.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.LabelField(propertyPosition, item.TypeId);

            DrawPreviewBox(rect, propertyPosition.width, defaultYPosition, gradeData?.Sprite);
        }

        private void DrawGradeElement(Rect rect, int grade, MergeGradeData gradeData)
        {
            float defaultYPosition = rect.y;

            rect.width -= 60;

            string displayName = gradeData?.DisplayName;

            Rect propertyPosition = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(propertyPosition, $"Grade {grade}");

            propertyPosition.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.LabelField(propertyPosition, displayName ?? string.Empty);

            DrawPreviewBox(rect, propertyPosition.width, defaultYPosition, gradeData?.Sprite);
        }

        private static void DrawPreviewBox(Rect rect, float labelWidth, float defaultYPosition, Sprite sprite)
        {
            Rect boxRect = new Rect(rect.x + labelWidth + 2, defaultYPosition, 58, 58);
            GUI.Box(boxRect, GUIContent.none);

            Texture2D previewTexture = sprite != null ? AssetPreview.GetAssetPreview(sprite) : null;
            if (previewTexture != null)
            {
                GUI.DrawTexture(new Rect(boxRect.x + 2, boxRect.y + 2, 55, 55), previewTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(boxRect.x + 2, boxRect.y + 2, 55, 55), EditorCustomStyles.GetMissingIcon());
            }
        }
    }
}
