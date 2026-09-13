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
    /// 构建每日登录奖励签到面板（DailyRewardPanel.prefab）+ 放入 Game.unity 主 Canvas。
    /// 幂等可重跑：菜单 Actions/Daily Reward/Rebuild Panel。
    /// </summary>
    public static class DailyRewardUIBuilder
    {
        private const string PREFAB_PATH = "Assets/Project Files/Game/Prefabs/UI/Canvas/DailyRewardPanel.prefab";
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string FONT_PATH = "Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50.asset";
        private const string NINE_SLICE = "Assets/Watermelon Core/Core Resources/Images/core_universal_back.png";
        private const string PANEL = "Assets/Project Files/Game/Art/UI/Common/panel.png";
        private const string BTN_PURPLE = "Assets/Project Files/Game/Art/UI/Common/btn_purple.png";

        [MenuItem("Actions/Daily Reward/Rebuild Panel")]
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

            Debug.Log($"[DailyReward] Panel rebuilt: {PREFAB_PATH}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
        }

        private static void PlaceInGameScene()
        {
            UIController uiController = Object.FindObjectOfType<UIController>();
            if (uiController == null)
            {
                Debug.LogError("[DailyReward] UIController not found in Game scene.");
                return;
            }

            Transform canvas = uiController.transform;
            Transform existing = canvas.Find("Daily Reward Panel");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
                PrefabUtility.InstantiatePrefab(prefab, canvas);

            Debug.Log("[DailyReward] Placed DailyRewardPanel under main canvas.");
        }

        private static GameObject BuildPanel()
        {
            GameObject root = new GameObject("Daily Reward Panel", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.layer = 5;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            StretchFull(rootRect);

            DailyRewardPanel component = root.AddComponent<DailyRewardPanel>();

            // Background mask
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            bg.raycastTarget = true;

            // Panel
            GameObject panel = CreateUIObject("Panel", root.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 560f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = LoadSprite(PANEL);
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 2f;
            panelImg.color = Color.white;

            // Title
            TMP_Text title = CreateTMP(panel.transform, "Title", "每日登录奖励", 48, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            // Slots container
            GameObject slotsGo = CreateUIObject("Slots", panel.transform);
            RectTransform slotsRect = slotsGo.GetComponent<RectTransform>();
            SetRect(slotsRect, new Vector2(0.06f, 0.40f), new Vector2(0.94f, 0.76f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup layout = slotsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Slot template
            GameObject slotTemplate = CreateUIObject("Slot Template", slotsGo.transform);
            slotTemplate.SetActive(false);
            LayoutElement slotLayout = slotTemplate.AddComponent<LayoutElement>();
            slotLayout.minWidth = 88f;
            slotLayout.minHeight = 140f;
            slotLayout.preferredWidth = 88f;
            Image slotBg = slotTemplate.AddComponent<Image>();
            slotBg.sprite = LoadSprite(NINE_SLICE);
            slotBg.type = Image.Type.Sliced;
            slotBg.pixelsPerUnitMultiplier = 2f;
            slotBg.color = CandyColors.MintLt;
            slotBg.raycastTarget = false;

            TMP_Text amount = CreateTMP(slotTemplate.transform, "Amount", "50", 34, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Center);
            SetRect(amount.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 0.95f), Vector2.zero, Vector2.zero);
            TMP_Text state = CreateTMP(slotTemplate.transform, "State", "1天", 26, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(state.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.35f), Vector2.zero, Vector2.zero);

            // Status
            TMP_Text statusText = CreateTMP(panel.transform, "Status", "登录即领，连续登录奖励更丰厚！", 30, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(statusText.rectTransform, new Vector2(0f, 0.20f), new Vector2(1f, 0.38f), Vector2.zero, Vector2.zero);

            // Claim button
            GameObject claimGo = CreateUIObject("Claim Button", panel.transform);
            RectTransform claimRect = claimGo.GetComponent<RectTransform>();
            SetRect(claimRect, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.18f), Vector2.zero, Vector2.zero);
            Image claimImg = claimGo.AddComponent<Image>();
            claimImg.sprite = LoadSprite(BTN_PURPLE);
            claimImg.type = Image.Type.Sliced;
            claimImg.pixelsPerUnitMultiplier = 2f;
            claimImg.color = CandyColors.StrawberryPri;
            claimImg.raycastTarget = true;
            Button claimButton = claimGo.AddComponent<Button>();
            claimButton.targetGraphic = claimImg;
            TMP_Text claimLabel = CreateTMP(claimGo.transform, "Text", "领取", 36, Color.white, TextAlignmentOptions.Center);
            StretchFull(claimLabel.rectTransform);

            // Bind
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("panelScalable").FindPropertyRelative("transform").objectReferenceValue = panelRect;
            so.FindProperty("claimButton").objectReferenceValue = claimButton;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("slotsContainer").objectReferenceValue = slotsRect;
            so.FindProperty("slotTemplate").objectReferenceValue = slotTemplate;
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
