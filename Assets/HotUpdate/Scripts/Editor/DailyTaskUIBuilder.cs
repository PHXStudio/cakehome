#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 构建每日任务面板（DailyTaskPanel.prefab）+ 放入 Game.unity 主 Canvas。
    /// 幂等可重跑：菜单 Actions/Daily Task/Rebuild Panel。
    /// </summary>
    public static class DailyTaskUIBuilder
    {
        private const string PREFAB_PATH = "Assets/Project Files/Game/Prefabs/UI/Canvas/DailyTaskPanel.prefab";
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string FONT_PATH = "Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50.asset";
        private const string NINE_SLICE = "Assets/Watermelon Core/Core Resources/Images/core_universal_back.png";
        private const string PANEL = "Assets/Project Files/Game/Art/UI/Common/panel.png";
        private const string BTN_PURPLE = "Assets/Project Files/Game/Art/UI/Common/btn_purple.png";
        private const string BTN_GREEN = "Assets/Project Files/Game/Art/UI/Common/btn_green.png";
        private const string BTN_CLOSE = "Assets/Project Files/Game/Art/UI/Common/btn_close.png";

        [MenuItem("Actions/Daily Task/Rebuild Panel")]
        public static void Rebuild()
        {
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = BuildPanel();
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
            PlaceInGameScene();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log($"[DailyTask] Panel rebuilt: {PREFAB_PATH}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
        }

        private static void PlaceInGameScene()
        {
            UIController uiController = Object.FindObjectOfType<UIController>();
            if (uiController == null)
            {
                Debug.LogError("[DailyTask] UIController not found in Game scene.");
                return;
            }

            Transform canvas = uiController.transform;
            Transform existing = canvas.Find("Daily Task Panel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
                PrefabUtility.InstantiatePrefab(prefab, canvas);

            Debug.Log("[DailyTask] Placed DailyTaskPanel under main canvas.");
        }

        private static GameObject BuildPanel()
        {
            GameObject root = new GameObject("Daily Task Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.layer = 5;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 201;
            StretchFull(root.GetComponent<RectTransform>());

            DailyTaskPanel component = root.AddComponent<DailyTaskPanel>();

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            bg.raycastTarget = true;

            GameObject panel = CreateUIObject("Panel", root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 640f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = LoadSprite(PANEL);
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 2f;
            panelImg.color = Color.white;

            TMP_Text title = CreateTMP(panel.transform, "Title", "每日任务", 48, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            Button closeBtn = CreateButton(panel.transform, "Close", LoadSprite(BTN_CLOSE), new Color(1f, 1f, 1f, 0.9f));
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.86f, 0.88f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);

            GameObject containerGo = CreateUIObject("Tasks", panel.transform);
            RectTransform containerRect = containerGo.GetComponent<RectTransform>();
            SetRect(containerRect, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject rowTemplate = CreateUIObject("Task Row Template", containerGo.transform);
            rowTemplate.SetActive(false);
            LayoutElement rowLayout = rowTemplate.AddComponent<LayoutElement>();
            rowLayout.minHeight = 92f;
            rowLayout.preferredHeight = 92f;
            Image rowBg = rowTemplate.AddComponent<Image>();
            rowBg.sprite = LoadSprite(NINE_SLICE);
            rowBg.type = Image.Type.Sliced;
            rowBg.pixelsPerUnitMultiplier = 2f;
            rowBg.color = new Color(1f, 1f, 1f, 0.92f);

            TMP_Text name = CreateTMP(rowTemplate.transform, "Name", "任务", 32, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Left);
            SetRect(name.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.55f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            TMP_Text progress = CreateTMP(rowTemplate.transform, "Progress", "0/3", 30, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(progress.rectTransform, new Vector2(0.55f, 0f), new Vector2(0.72f, 1f), Vector2.zero, Vector2.zero);

            GameObject claimGo = CreateUIObject("Claim", rowTemplate.transform);
            RectTransform claimRect = claimGo.GetComponent<RectTransform>();
            SetRect(claimRect, new Vector2(0.76f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            Image claimImg = claimGo.AddComponent<Image>();
            claimImg.sprite = LoadSprite(BTN_GREEN);
            claimImg.type = Image.Type.Sliced;
            claimImg.pixelsPerUnitMultiplier = 2f;
            claimImg.color = CandyColors.MintPri;
            claimImg.raycastTarget = true;
            Button claimButton = claimGo.AddComponent<Button>();
            claimButton.targetGraphic = claimImg;
            TMP_Text claimLabel = CreateTMP(claimGo.transform, "Text", "领取", 28, Color.white, TextAlignmentOptions.Center);
            StretchFull(claimLabel.rectTransform);

            SerializedObject so = new SerializedObject(component);
            so.FindProperty("panelScalable").FindPropertyRelative("transform").objectReferenceValue = panelRect;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("taskContainer").objectReferenceValue = containerRect;
            so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return root;
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Button CreateButton(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.color = color;
            img.raycastTarget = true;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static TMP_Text CreateTMP(Transform parent, string name, string value, int size, Color color, TextAlignmentOptions align)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset LoadFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        }
    }
}
#endif
