#pragma warning disable 649

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Watermelon
{
    public class LevelEditorWindow : LevelEditorBase
    {

        //used variables
        private const string LEVELS_PROPERTY_NAME = "zones";
        private const string HALF_LOCKED_ITEM_SPRITE_PROPERTY_NAME = "halfLockedItemSprite";
        private const string LOCKED_ITEM_SPRITE_PROPERTY_NAME = "lockedItemSprite";
        private const string MERGE_LEVEL_DATA_PROPERTY_NAME = "mergeLevelData";
        private const string GRID_WIDTH_PROPERTY_NAME = "gridWidth";
        private const string GRID_HEIGHT_PROPERTY_NAME = "gridHeight";
        private const string CELLS_PROPERTY_NAME = "cells";
        private const string TYPE_ID_PROPERTY_NAME = "typeId";
        private const string GRADE_PROPERTY_NAME = "grade";
        private const string LOCK_STATE_PROPERTY_NAME = "lockState";
        private SerializedProperty levelsSerializedProperty;
        private Texture2D halfLockedItemTexture2D;
        private Texture2D lockedItemSpriteTexture2D;
        private LevelRepresentation selectedLevelRepresentation;
        private LevelsHandler levelsHandler;

        // The merge board is a single level shared by the whole game (LevelDatabase.mergeLevelData),
        // so the grid painter is bound here, independent of which zone row is selected.
        private SerializedProperty mergeLevelDataProperty;
        private SerializedObject mergeLevelSerializedObject;
        private SerializedProperty gridWidthProperty;
        private SerializedProperty gridHeightProperty;
        private SerializedProperty cellsProperty;

        private bool HasSharedLevel => mergeLevelDataProperty != null && mergeLevelDataProperty.objectReferenceValue != null;

        //sidebar
        private const int SIDEBAR_WIDTH = 320;
        //PlayerPrefs
        private const string PREFS_LEVEL = "editor_level_index";
        private const string PREFS_WIDTH = "editor_sidebar_width";
        private const string PREFS_VISUALS = "editor_visuals_locked";

        //level drawing
        private Rect drawRect;
        private float xSize;
        private float ySize;
        private float elementSize;
        private Event currentEvent;
        private Vector2 elementUnderMouseIndex;
        private Vector2Int elementPosition;
        private int invertedY;
        private float buttonRectX;
        private float buttonRectY;
        private Rect buttonRect;

        List<ItemRepresentation> items;

        //Menu
        private Rect separatorRect;
        private bool separatorIsDragged;
        private int currentSideBarWidth;
        private bool lastActiveLevelOpened;

        //Selection
        private int selectedCellIndex;
        private Rect lineRect;

        private bool showLockedVisuals; 

        protected override WindowConfiguration SetUpWindowConfiguration(WindowConfiguration.Builder builder)
        {
            return builder.SetWindowMinSize(new Vector2(700, 500)).Build();
        }

        protected override Type GetLevelsDatabaseType()
        {
            return typeof(LevelDatabase);
        }

        public override Type GetLevelType()
        {
            return typeof(ZoneData);
        }

        public override string ELEMENT_ASSET_PREFIX => "New Zone ";
        public override string ELEMENT_FOLDER_PATH => LEVELS_DATABASE_FOLDER_PATH;

        protected override void ReadLevelDatabaseFields()
        {
            levelsSerializedProperty = levelsDatabaseSerializedObject.FindProperty(LEVELS_PROPERTY_NAME);
            halfLockedItemTexture2D = (levelsDatabaseSerializedObject.FindProperty(HALF_LOCKED_ITEM_SPRITE_PROPERTY_NAME).objectReferenceValue as Sprite).texture;
            lockedItemSpriteTexture2D = (levelsDatabaseSerializedObject.FindProperty(LOCKED_ITEM_SPRITE_PROPERTY_NAME).objectReferenceValue as Sprite).texture;

            mergeLevelDataProperty = levelsDatabaseSerializedObject.FindProperty(MERGE_LEVEL_DATA_PROPERTY_NAME);
            ReadSharedLevelFields();
        }

        private void ReadSharedLevelFields()
        {
            if (HasSharedLevel)
            {
                mergeLevelSerializedObject = new SerializedObject(mergeLevelDataProperty.objectReferenceValue);
                gridWidthProperty = mergeLevelSerializedObject.FindProperty(GRID_WIDTH_PROPERTY_NAME);
                gridHeightProperty = mergeLevelSerializedObject.FindProperty(GRID_HEIGHT_PROPERTY_NAME);
                cellsProperty = mergeLevelSerializedObject.FindProperty(CELLS_PROPERTY_NAME);
            }
            else
            {
                mergeLevelSerializedObject = null;
                gridWidthProperty = null;
                gridHeightProperty = null;
                cellsProperty = null;
            }
        }

        protected override void InitializeVariables()
        {
            items = new List<ItemRepresentation>();
            LoadDataFromMergeDatabase();
            currentSideBarWidth = PlayerPrefs.GetInt(PREFS_WIDTH, SIDEBAR_WIDTH);
            showLockedVisuals = PlayerPrefs.GetInt(PREFS_VISUALS, 1) == 1;
            selectedCellIndex = -1;
        }

        private void LoadDataFromMergeDatabase()
        {
            MergeDatabase mergeDatabase = EditorUtils.GetAsset<MergeDatabase>();

            if(mergeDatabase == null)
            {
                Debug.Log("MergeDatabase not found. ");
                return;
            }

            items.Clear();
            SerializedObject serializedObject = new SerializedObject(mergeDatabase);
            SerializedProperty itemsProperty = serializedObject.FindProperty("items");

            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                items.Add(new ItemRepresentation(itemsProperty.GetArrayElementAtIndex(i)));
            }
            
        }

        private void OpenLastActiveLevel()
        {
            if (!lastActiveLevelOpened)
            {
                if ((levelsSerializedProperty.arraySize > 0) && PlayerPrefs.HasKey(PREFS_LEVEL))
                {
                    int levelIndex = Mathf.Clamp(PlayerPrefs.GetInt(PREFS_LEVEL, 0), 0, levelsSerializedProperty.arraySize - 1);
                    levelsHandler.CustomList.SelectedIndex = levelIndex;
                    levelsHandler.OpenLevel(levelIndex);
                }

                lastActiveLevelOpened = true;
            }
        }


        protected override void Styles()
        {
            if(levelsDatabase != null)
            {
                levelsHandler = new LevelsHandler(levelsDatabaseSerializedObject, levelsSerializedProperty);
            }
        }

        public override void OpenLevel(UnityEngine.Object levelObject, int index)
        {
            PlayerPrefs.SetInt(PREFS_LEVEL, index);
            PlayerPrefs.Save();
            selectedLevelRepresentation = new LevelRepresentation(levelObject);
            selectedCellIndex = -1;
        }

        public override string GetLevelLabel(UnityEngine.Object levelObject, int index)
        {
            return new LevelRepresentation(levelObject).GetLevelLabel(index, stringBuilder);
        }

        public override void ClearLevel(UnityEngine.Object levelObject)
        {
            SetupNewZone(levelObject as ZoneData);
        }

        private void SetupNewZone(ZoneData zone)
        {
            if (zone == null) return;

            int zoneNumber = GetNextZoneNumber();
            string zoneFolderName = "Zone " + zoneNumber;
            string zoneFolderPath = LEVELS_DATABASE_FOLDER_PATH + PATH_SEPARATOR + zoneFolderName;

            if (!AssetDatabase.IsValidFolder(zoneFolderPath))
            {
                AssetDatabase.CreateFolder(LEVELS_DATABASE_FOLDER_PATH, zoneFolderName);
            }

            string oldZonePath = AssetDatabase.GetAssetPath(zone);
            string newZonePath = AssetDatabase.GenerateUniqueAssetPath(zoneFolderPath + PATH_SEPARATOR + zoneFolderName + " Data.asset");
            string moveError = AssetDatabase.MoveAsset(oldZonePath, newZonePath);

            if (!string.IsNullOrEmpty(moveError))
            {
                Debug.LogError($"Failed to move zone asset to '{newZonePath}': {moveError}");
            }

            AssetDatabase.SaveAssets();
        }

        private int GetNextZoneNumber()
        {
            string[] guids = AssetDatabase.FindAssets("t:ZoneData", new[] { LEVELS_DATABASE_FOLDER_PATH });
            int maxNumber = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^Zone (\d+) Data$");

                if (match.Success)
                {
                    int number = int.Parse(match.Groups[1].Value);
                    if (number > maxNumber) maxNumber = number;
                }
            }

            return maxNumber + 1;
        }
        public override void LogErrorsForGlobalValidation(UnityEngine.Object levelObject, int index)
        {
            LevelRepresentation level = new LevelRepresentation(levelObject);
            level.ValidateLevel();

            if (!level.IsLevelCorrect)
            {
                Debug.Log("Logging validation errors for level #" + (index + 1) + " :");

                foreach (string error in level.errorLabels)
                {
                    Debug.LogWarning(error);
                }
            }
            else
            {
                Debug.Log($"Level # {(index + 1)} passed validation.");
            }
        }

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DisplayListArea();
            HandleChangingSideBar();
            DisplayMainArea();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void HandleChangingSideBar()
        {
            separatorRect = EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(0), GUILayout.ExpandHeight(true));
            EditorGUILayout.EndHorizontal();
            separatorRect.xMin -= GUI.skin.box.margin.right;
            separatorRect.xMax += GUI.skin.box.margin.left;
            EditorGUIUtility.AddCursorRect(separatorRect, MouseCursor.ResizeHorizontal);


            if (separatorRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    separatorIsDragged = true;
                    levelsHandler.IgnoreDragEvents = true;
                    Event.current.Use();
                }
            }

            if (separatorIsDragged)
            {
                if (Event.current.type == EventType.MouseUp)
                {
                    separatorIsDragged = false;
                    levelsHandler.IgnoreDragEvents = false;
                    PlayerPrefs.SetInt(PREFS_WIDTH, currentSideBarWidth);
                    PlayerPrefs.Save();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    currentSideBarWidth = Mathf.RoundToInt(Event.current.delta.x) + currentSideBarWidth;
                    Event.current.Use();
                }
            }
        }

        private void DisplayListArea()
        {
            OpenLastActiveLevel();
            EditorGUILayout.BeginVertical(GUILayout.Width(currentSideBarWidth));
            levelsHandler.DisplayReordableList();
            DisplaySelecteditem();
            DisplayRefreshEditorButton();

            EditorGUILayout.EndVertical();
        }

        private void DisplaySelecteditem()
        {
            if (selectedCellIndex == -1)
            {
                return;
            }

            if(selectedLevelRepresentation == null)
            {
                return;
            }

            if (cellsProperty == null)
            {
                return;
            }

            SerializedProperty cellProperty = cellsProperty.GetArrayElementAtIndex(selectedCellIndex);

            SerializedProperty iterator = cellProperty.Copy();
            SerializedProperty endProperty = cellProperty.GetEndProperty();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                if (iterator.name.Equals("grade"))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Grade", GUILayout.Width(EditorGUIUtility.labelWidth));

                    if (GUILayout.Button("-"))
                    {
                        iterator.intValue--;
                    }

                    iterator.intValue = EditorGUILayout.IntField(iterator.intValue);

                    if (GUILayout.Button("+"))
                    {
                        iterator.intValue++;
                    }

                    if(iterator.intValue < 1)
                    {
                        iterator.intValue = 1;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    enterChildren = false;
                    EditorGUILayout.PropertyField(iterator, true);
                }



            }
        }

        private void DisplayMainArea()
        {

            if (levelsHandler.SelectedLevelIndex == -1)
            {
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (IsPropertyChanged(levelsHandler.SelectedLevelProperty, new GUIContent("File")))
            {
                levelsHandler.ReopenLevel();
            }

            if (selectedLevelRepresentation.NullLevel)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("Zone", selectedLevelRepresentation.zoneNameProperty.stringValue);

            if (!HasSharedLevel)
            {
                EditorGUILayout.HelpBox("Level Database has no shared Merge Level Data yet.", MessageType.Warning);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(mergeLevelDataProperty, new GUIContent("Merge Level Data"));
                if (EditorGUI.EndChangeCheck())
                {
                    levelsDatabaseSerializedObject.ApplyModifiedProperties();
                    ReadSharedLevelFields();
                }

                if (GUILayout.Button("Create Level", EditorCustomStyles.button))
                {
                    CreateSharedLevel();
                }

                EditorGUILayout.EndVertical();
                return;
            }

            if (IsPropertyChanged(gridWidthProperty))
            {
                HandleSizePropertyChange();
            }

            if (IsPropertyChanged(gridHeightProperty))
            {
                HandleSizePropertyChange();
            }

            EditorGUI.BeginChangeCheck();

            showLockedVisuals = EditorGUILayout.ToggleLeft("Show locked visuals", showLockedVisuals);

            if (EditorGUI.EndChangeCheck())
            {
                PlayerPrefs.SetInt(PREFS_VISUALS, showLockedVisuals? 1 : 0);
                PlayerPrefs.Save();
            }

            DrawLevel();

            levelsHandler.UpdateCurrentLevelLabel(selectedLevelRepresentation.GetLevelLabel(levelsHandler.SelectedLevelIndex, stringBuilder));
            selectedLevelRepresentation.ApplyChanges();
            mergeLevelSerializedObject?.ApplyModifiedProperties();

            EditorGUILayout.EndVertical();
        }

        private void CreateSharedLevel()
        {
            MergeLevelData newLevel = CreateLevelAsset(LEVELS_DATABASE_FOLDER_PATH, "Merge Level");

            AssetDatabase.SaveAssets();

            mergeLevelDataProperty.objectReferenceValue = newLevel;
            levelsDatabaseSerializedObject.ApplyModifiedProperties();
            ReadSharedLevelFields();
        }

        private MergeLevelData CreateLevelAsset(string folderPath, string levelBaseName)
        {
            string levelPath = AssetDatabase.GenerateUniqueAssetPath(folderPath + PATH_SEPARATOR + levelBaseName + ".asset");

            MergeLevelData newLevel = ScriptableObject.CreateInstance<MergeLevelData>();
            AssetDatabase.CreateAsset(newLevel, levelPath);

            SerializedObject newLevelSerializedObject = new SerializedObject(newLevel);
            SerializedProperty newGridWidth = newLevelSerializedObject.FindProperty("gridWidth");
            SerializedProperty newGridHeight = newLevelSerializedObject.FindProperty("gridHeight");
            SerializedProperty newCells = newLevelSerializedObject.FindProperty("cells");
            newCells.arraySize = newGridWidth.intValue * newGridHeight.intValue;
            newLevelSerializedObject.ApplyModifiedProperties();

            return newLevel;
        }

        private void DrawLevel()
        {
            drawRect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            xSize = Mathf.Floor(drawRect.width / gridWidthProperty.intValue);
            ySize = Mathf.Floor(drawRect.height / gridHeightProperty.intValue);
            elementSize = Mathf.Min(xSize, ySize);
            currentEvent = Event.current;
            int index;
            int x;
            int y;
            string id;
            int grade;

            //Handle drag and click
            if (currentEvent.type == EventType.MouseDown)
            {
                elementUnderMouseIndex = (currentEvent.mousePosition - drawRect.position) / (elementSize);

                elementPosition = new Vector2Int(Mathf.FloorToInt(elementUnderMouseIndex.x), gridHeightProperty.intValue - 1 - Mathf.FloorToInt(elementUnderMouseIndex.y));

                if ((elementPosition.x >= 0) && (elementPosition.x < gridWidthProperty.intValue) && (elementPosition.y >= 0) && (elementPosition.y < gridHeightProperty.intValue))
                {
                    if (currentEvent.button == 0)
                    {
                        index = GetIndex(elementPosition.x, elementPosition.y);
                        selectedCellIndex = index;
                        currentEvent.Use();
                    }
                    else if ((currentEvent.button == 1) && (currentEvent.type == EventType.MouseDown))
                    {
                        index = GetIndex(elementPosition.x, elementPosition.y);
                        GenericMenu menu = new GenericMenu();
                        id = GetTypeId(index);

                        for (int i = 0; i < items.Count; i++)
                        {
                            if (id.Equals(items[i].id))
                            {
                                menu.AddDisabledItem(new GUIContent(items[i].id));
                            }
                            else
                            {
                                menu.AddItem(new GUIContent(items[i].id), false, HandleMenuFunction, new Vector2Int(index,i));
                            }

                        }

                        menu.ShowAsContext();
                        currentEvent.Use();
                    }
                }
            }

            //draw grid
            for (int xLineIndex = 0; xLineIndex < gridWidthProperty.intValue + 1; xLineIndex++)
            {
                lineRect = new Rect(drawRect.x + (xLineIndex * elementSize), drawRect.y, 2, gridHeightProperty.intValue * elementSize);
                DrawColorRect(lineRect, Color.gray);
            }

            for (int yLineIndex = 0; yLineIndex < gridHeightProperty.intValue + 1; yLineIndex++)
            {
                lineRect = new Rect(drawRect.x, drawRect.y + (yLineIndex * elementSize), gridWidthProperty.intValue * elementSize, 2);
                DrawColorRect(lineRect, Color.gray);
            }

            x = 0;
            y = 0;

            for (int cellIndex = 0; cellIndex < cellsProperty.arraySize; cellIndex++)
            {
                invertedY = gridHeightProperty.intValue - 1 - y;
                id = GetTypeId(cellIndex);
                grade = Mathf.Clamp( GetGrade(cellIndex) - 1, 0, int.MaxValue);
                buttonRectX = drawRect.position.x + x * elementSize;
                buttonRectY = drawRect.position.y + invertedY * elementSize;
                buttonRect = new Rect(buttonRectX, buttonRectY, elementSize, elementSize);

                for (int j = 0; j < items.Count; j++)
                {
                    if (items[j].id.Equals(id))
                    {
                        if (showLockedVisuals)
                        {
                            CellLockState lockState = GetLockState(cellIndex);

                            if (lockState == CellLockState.Locked)
                            {
                                GUI.DrawTexture(buttonRect, lockedItemSpriteTexture2D);
                            }
                            else
                            {
                                if (grade >= items[j].textures.Length)
                                {
                                    Debug.LogError($"Incorrect grade for cell with red texture.");
                                    DrawColorRect(buttonRect, Color.red);
                                }
                                else
                                {
                                    GUI.DrawTexture(buttonRect, items[j].textures[grade]);

                                    if (lockState == CellLockState.HalfLocked)
                                    {
                                        DrawColorRect(buttonRect, Color.black.SetAlpha(0.3f));
                                        GUI.DrawTexture(buttonRect, halfLockedItemTexture2D);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (grade >= items[j].textures.Length)
                            {
                                Debug.LogError($"Incorrect grade for cell with red texture.");
                                DrawColorRect(buttonRect, Color.red);
                            }
                            else
                            {
                                GUI.DrawTexture(buttonRect, items[j].textures[grade]);
                            }
                        }
                    }

                }

                if(cellIndex == selectedCellIndex)
                {
                    GUI.DrawTexture(buttonRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 1.0f, Color.blue, 2f, 0f);
                }

                x++;

                if (x == gridWidthProperty.intValue)
                {
                    y++;
                    x = 0;
                }

            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }

        private void HandleMenuFunction(object userData)
        {
            Vector2Int data = (Vector2Int)userData;
            SetTypeId(data.x, items[data.y].id);
        }

        private int GetIndex(int x, int y)
        {
            return y * gridWidthProperty.intValue + x;
        }

        private string GetTypeId(int index)
        {
            return cellsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(TYPE_ID_PROPERTY_NAME).stringValue;
        }

        private void SetTypeId(int index, string value)
        {
            cellsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(TYPE_ID_PROPERTY_NAME).stringValue = value;
        }

        private int GetGrade(int index)
        {
            return cellsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(GRADE_PROPERTY_NAME).intValue;
        }

        private CellLockState GetLockState(int index)
        {
            return (CellLockState)cellsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(LOCK_STATE_PROPERTY_NAME).enumValueIndex;
        }

        private void HandleSizePropertyChange()
        {
            if (gridWidthProperty.intValue < 5)
            {
                gridWidthProperty.intValue = 5;
            }

            if (gridHeightProperty.intValue < 5)
            {
                gridHeightProperty.intValue = 5;
            }

            cellsProperty.arraySize = 0;

            for (int i = 0; i < gridWidthProperty.intValue * gridHeightProperty.intValue; i++)
            {
                cellsProperty.arraySize++;
            }
        }

        public override void OnBeforeAssemblyReload()
        {
            lastActiveLevelOpened = false;
        }


        public override bool WindowClosedInPlaymode()
        {
            return false;
        }

        private class ItemRepresentation
        {
            public string id;
            public MergeItemType itemType;
            public Texture2D[] textures;

            public ItemRepresentation (SerializedProperty itemPropety)
            {
                this.id = itemPropety.FindPropertyRelative("typeId").stringValue;
                this.itemType = (MergeItemType)itemPropety.FindPropertyRelative("itemType").intValue;

                SerializedProperty array = itemPropety.FindPropertyRelative("grades");
                textures = new Texture2D[array.arraySize];

                for (int i = 0; i < textures.Length; i++)
                {
                    if(array.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue == null)
                    {
                        Debug.LogError($"Item {id} have null sprite in grade #{(i + 1).ToString()}.");
                        textures[i] = EditorGUIUtility.whiteTexture;
                    }
                    else
                    {
                        textures[i] = (array.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue as Sprite).texture;
                    }
                }
            }
        }

        protected class LevelRepresentation : LevelRepresentationBase
        {
            private const string ZONE_NAME_PROPERTY_NAME = "zoneName";

            public SerializedProperty zoneNameProperty;
            public bool checkPassedCached;
            public string checkStatus;

            protected override bool LEVEL_CHECK_ENABLED => false;

            public LevelRepresentation(UnityEngine.Object levelObject) : base(levelObject)
            {
            }

            protected override void ReadFields()
            {
                zoneNameProperty = serializedLevelObject.FindProperty(ZONE_NAME_PROPERTY_NAME);
            }

            public override void Clear()
            {
                // Freshly created zone — nothing to reset.
            }

            public override string GetLevelLabel(int index, StringBuilder stringBuilder)
            {
                stringBuilder.Clear();
                stringBuilder.Append(NUMBER);
                stringBuilder.Append(index + 1);
                stringBuilder.Append(SEPARATOR);

                if (NullLevel)
                {
                    stringBuilder.Append(NULL_FILE);
                }
                else
                {
                    stringBuilder.Append(string.IsNullOrEmpty(zoneNameProperty.stringValue) ? levelObject.name : zoneNameProperty.stringValue);
                }

                return stringBuilder.ToString();
            }
        }

    }
}