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
    /// 构建 TAB3 我的页面：Avatar 换装 + 称号 + 甜品展馆。替换 Game.unity 的 "UI Profile Page"。
    /// 幂等可重跑。参考 ShopUIBuilder 模式。
    /// </summary>
    public static class ProfileUIBuilder
    {
        private const string PREFAB_PATH = "Assets/Project Files/Game/Prefabs/UI/Canvas/UIProfilePage.prefab";
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string PAGE_NAME = "UI Profile Page";

        private const string FONT_PATH = "Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50.asset";
        private const string NINE_SLICE = "Assets/Watermelon Core/Core Resources/Images/core_universal_back.png";
        private const string BTN_PURPLE = "Assets/Project Files/Game/Art/UI/Common/btn_purple.png";
        private const string BTN_GRAY = "Assets/Project Files/Game/Art/UI/Common/btn_gray.png";

        [MenuItem("Actions/Profile/Rebuild Profile Page UI")]
        public static void Rebuild()
        {
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = BuildPage();
            BindPageReferences(root);
            root.transform.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            // Defensive: Unity can serialize prefab root scale as 0; force it back to 1.
            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (saved != null && saved.transform.localScale != Vector3.one)
            {
                saved.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(saved);
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
            ReplacePageInGameScene();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log($"[ProfileUI] UIProfilePage.prefab rebuilt + Game.unity page replaced.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
        }

        private static void ReplacePageInGameScene()
        {
            UIController uiController = Object.FindObjectOfType<UIController>();
            if (uiController == null)
            {
                Debug.LogError("[ProfileUI] UIController not found in Game scene.");
                return;
            }

            Transform canvas = uiController.transform;

            // 删除所有匹配的旧页面（避免多次 Rebuild 产生重复实例）
            var oldPages = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child.name.StartsWith(PAGE_NAME) || child.name.Contains("Profile"))
                    oldPages.Add(child);
            }

            foreach (Transform old in oldPages)
                Object.DestroyImmediate(old.gameObject);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[ProfileUI] Prefab missing: {PREFAB_PATH}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas) as GameObject;
            if (instance != null)
                StretchFull(instance.GetComponent<RectTransform>());

            Debug.Log("[ProfileUI] Game.unity: replaced 'UI Profile Page' with prefab instance.");
        }

        // ------------------------------------------------------------------ Build

        private static GameObject BuildPage()
        {
            GameObject page = new GameObject(PAGE_NAME, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            page.layer = 5;
            StretchFull(page.GetComponent<RectTransform>());
            Canvas pageCanvas = page.GetComponent<Canvas>();
            pageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pageCanvas.overrideSorting = true;
            pageCanvas.sortingOrder = 40;

            UIProfilePage component = page.AddComponent<UIProfilePage>();

            // Background
            GameObject bg = CreateUIObject("Background", page.transform);
            StretchFull(bg.GetComponent<RectTransform>());
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.14f, 0.12f, 0.16f, 1f);
            bgImg.raycastTarget = true;

            // Safe Area
            RectTransform safeArea = CreateUIObject("Safe Area", page.transform).GetComponent<RectTransform>();
            StretchFull(safeArea);
            safeArea.offsetMin = new Vector2(0f, BottomNavLayout.Height);

            // Title
            TMP_Text title = CreateTMP(safeArea, "Title", "我的", 56, Color.white, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero);

            // Avatar section
            GameObject avatarSection = CreateUIObject("Avatar Section", safeArea);
            RectTransform avatarRect = avatarSection.GetComponent<RectTransform>();
            SetRect(avatarRect, new Vector2(0.03f, 0.55f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);
            Image avatarBg = avatarSection.AddComponent<Image>();
            avatarBg.sprite = LoadSprite(NINE_SLICE);
            avatarBg.type = Image.Type.Sliced;
            avatarBg.pixelsPerUnitMultiplier = 2f;
            avatarBg.color = new Color(1f, 1f, 1f, 0.2f);

            TMP_Text avatarTitle = CreateTMP(avatarSection.transform, "Section Title", "店长换装", 34, Color.white, TextAlignmentOptions.Center);
            SetRect(avatarTitle.rectTransform, new Vector2(0f, 0.8f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            Transform avatarSlotRoot = CreateUIObject("Slots Root", avatarSection.transform).transform;
            RectTransform avatarSlotRect = avatarSlotRoot.GetComponent<RectTransform>();
            SetRect(avatarSlotRect, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.76f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup avatarVlg = avatarSlotRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            avatarVlg.spacing = 6f;
            avatarVlg.childAlignment = TextAnchor.UpperCenter;
            avatarVlg.childControlWidth = true;
            avatarVlg.childControlHeight = false;
            avatarVlg.childForceExpandHeight = false;

            GameObject avatarRowTemplate = BuildAvatarRowTemplate(avatarSlotRoot);

            // Title section (称号)
            GameObject titleSection = CreateUIObject("Title Section", safeArea);
            RectTransform titleRect = titleSection.GetComponent<RectTransform>();
            SetRect(titleRect, new Vector2(0.03f, 0.28f), new Vector2(0.97f, 0.53f), Vector2.zero, Vector2.zero);
            Image titleBg = titleSection.AddComponent<Image>();
            titleBg.sprite = LoadSprite(NINE_SLICE);
            titleBg.type = Image.Type.Sliced;
            titleBg.pixelsPerUnitMultiplier = 2f;
            titleBg.color = new Color(1f, 1f, 1f, 0.2f);

            TMP_Text titleSectionTitle = CreateTMP(titleSection.transform, "Section Title", "专属称号", 34, Color.white, TextAlignmentOptions.Center);
            SetRect(titleSectionTitle.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);

            Transform titleRoot = CreateUIObject("Titles Root", titleSection.transform).transform;
            RectTransform titleRootRect = titleRoot.GetComponent<RectTransform>();
            SetRect(titleRootRect, new Vector2(0.02f, 0.06f), new Vector2(0.98f, 0.68f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup titleVlg = titleRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            titleVlg.spacing = 6f;
            titleVlg.childAlignment = TextAnchor.UpperCenter;
            titleVlg.childControlWidth = true;
            titleVlg.childControlHeight = false;
            titleVlg.childForceExpandHeight = false;

            GameObject titleRowTemplate = BuildTitleRowTemplate(titleRoot);

            // Gallery section (展馆)
            GameObject gallerySection = CreateUIObject("Gallery Section", safeArea);
            RectTransform galleryRect = gallerySection.GetComponent<RectTransform>();
            SetRect(galleryRect, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.26f), Vector2.zero, Vector2.zero);
            Image galleryBg = gallerySection.AddComponent<Image>();
            galleryBg.sprite = LoadSprite(NINE_SLICE);
            galleryBg.type = Image.Type.Sliced;
            galleryBg.pixelsPerUnitMultiplier = 2f;
            galleryBg.color = new Color(1f, 1f, 1f, 0.2f);

            TMP_Text galleryText = CreateTMP(gallerySection.transform, "Gallery Text", "甜品展馆 0/0", 30, Color.white, TextAlignmentOptions.Center);
            StretchFull(galleryText.rectTransform);

            // Bind
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("safeAreaRectTransform").objectReferenceValue = safeArea;
            so.FindProperty("avatarTitleText").objectReferenceValue = avatarTitle;
            so.FindProperty("avatarSlotRoot").objectReferenceValue = avatarSlotRoot;
            so.FindProperty("avatarRowTemplate").objectReferenceValue = avatarRowTemplate;
            so.FindProperty("titleRoot").objectReferenceValue = titleRoot;
            so.FindProperty("titleRowTemplate").objectReferenceValue = titleRowTemplate;
            so.FindProperty("galleryCountText").objectReferenceValue = galleryText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return page;
        }

        private static void BindPageReferences(GameObject root)
        {
            // All bindings done in BuildPage.
        }

        private static GameObject BuildAvatarRowTemplate(Transform parent)
        {
            GameObject row = CreateUIObject("Avatar Row Template", parent);
            row.SetActive(false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 42f);
            Image rowImg = row.AddComponent<Image>();
            rowImg.sprite = LoadSprite(NINE_SLICE);
            rowImg.type = Image.Type.Sliced;
            rowImg.pixelsPerUnitMultiplier = 2f;
            rowImg.color = new Color(1f, 1f, 1f, 0.25f);

            TMP_Text nameText = CreateTMP(row.transform, "Name Text", "", 26, Color.white, TextAlignmentOptions.Left);
            SetRect(nameText.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text statusText = CreateTMP(row.transform, "Status Text", "", 24, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Center);
            SetRect(statusText.rectTransform, new Vector2(0.55f, 0f), new Vector2(0.78f, 1f), Vector2.zero, Vector2.zero);

            GameObject buyGo = CreateUIObject("Buy Button", row.transform);
            RectTransform buyRect = buyGo.GetComponent<RectTransform>();
            SetRect(buyRect, new Vector2(0.8f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero);
            Button buyBtn = CreateButtonVisual(buyGo, LoadSprite(BTN_PURPLE), new Color(0.9f, 0.5f, 0.6f));
            TMP_Text buyLabel = CreateTMP(buyGo.transform, "Text", "购买", 24, Color.white, TextAlignmentOptions.Center);
            StretchFull(buyLabel.rectTransform);

            // Bind AvatarSlotRow
            AvatarSlotRow rowComp = row.AddComponent<AvatarSlotRow>();
            SerializedObject so = new SerializedObject(rowComp);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("buyButton").objectReferenceValue = buyBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        private static GameObject BuildTitleRowTemplate(Transform parent)
        {
            GameObject row = CreateUIObject("Title Row Template", parent);
            row.SetActive(false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 38f);
            Image rowImg = row.AddComponent<Image>();
            rowImg.sprite = LoadSprite(NINE_SLICE);
            rowImg.type = Image.Type.Sliced;
            rowImg.pixelsPerUnitMultiplier = 2f;
            rowImg.color = new Color(1f, 1f, 1f, 0.2f);

            TMP_Text nameText = CreateTMP(row.transform, "Name Text", "", 26, Color.white, TextAlignmentOptions.Left);
            SetRect(nameText.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.7f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text statusText = CreateTMP(row.transform, "Status Text", "", 24, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.Right);
            SetRect(statusText.rectTransform, new Vector2(0.7f, 0f), new Vector2(0.98f, 1f), Vector2.zero, Vector2.zero);

            AvatarTitleRow rowComp = row.AddComponent<AvatarTitleRow>();
            SerializedObject so = new SerializedObject(rowComp);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return row;
        }

        // ------------------------------------------------------------------ Helpers (mirror ShopUIBuilder)

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
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

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset LoadFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
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
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static Button CreateButtonVisual(GameObject go, Sprite sprite, Color color)
        {
            Image img = go.GetComponent<Image>();
            if (img == null)
                img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.color = color;
            img.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            if (btn == null)
                btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }
    }
}
#endif
