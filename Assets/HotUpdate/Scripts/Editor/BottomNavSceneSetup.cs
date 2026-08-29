#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public static class BottomNavSceneSetup
    {
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const float NAV_HEIGHT = BottomNavLayout.Height;

        [MenuItem("Actions/Setup Bottom Navigation")]
        public static void Setup()
        {
            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            UIController uiController = Object.FindObjectOfType<UIController>();
            if (uiController == null)
            {
                Debug.LogError("[BottomNav] UIController not found in Game scene.");
                return;
            }

            Transform canvas = uiController.transform;

            if (canvas.GetComponentInChildren<UIProfilePage>(true) == null)
                EnsureProfilePage(canvas);
            if (canvas.GetComponentInChildren<UIBottomNavBar>(true) == null)
                EnsureBottomNav(canvas);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[BottomNav] Bottom navigation setup complete.");
        }

        private static void EnsureProfilePage(Transform canvas)
        {
            GameObject page = CreatePageRoot(canvas, "UI Profile Page", createBackground: true,
                out RectTransform safeArea, out Text title, out Text subtitle);
            UIProfilePage component = page.AddComponent<UIProfilePage>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("safeAreaRectTransform").objectReferenceValue = safeArea;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("subtitleText").objectReferenceValue = subtitle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePageRoot(Transform canvas, string name, bool createBackground,
            out RectTransform safeArea, out Text title, out Text subtitle)
        {
            GameObject page = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            page.layer = 5;
            page.transform.SetParent(canvas, false);
            StretchFull(page.GetComponent<RectTransform>());

            Canvas pageCanvas = page.GetComponent<Canvas>();
            pageCanvas.overrideSorting = true;
            pageCanvas.sortingOrder = 50;

            if (createBackground)
            {
                GameObject bg = CreateUIObject("Background", page.transform);
                StretchFull(bg.GetComponent<RectTransform>());
                bg.AddComponent<Image>().color = new Color(0.93f, 0.95f, 0.92f, 1f);
            }

            GameObject safe = CreateUIObject("Safe Area", page.transform);
            safeArea = safe.GetComponent<RectTransform>();
            StretchFull(safeArea);
            safeArea.offsetMin = new Vector2(0f, NAV_HEIGHT);

            title = CreateLabel(safe.transform, "Title", new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.7f));
            subtitle = CreateLabel(safe.transform, "Subtitle", new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.55f));
            return page;
        }

        private static void EnsureBottomNav(Transform canvas)
        {
            GameObject navRoot = new GameObject("Bottom Nav Bar", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            navRoot.layer = 5;
            navRoot.transform.SetParent(canvas, false);

            RectTransform navRect = navRoot.GetComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0f, 0f);
            navRect.anchorMax = new Vector2(1f, 0f);
            navRect.pivot = new Vector2(0.5f, 0f);
            navRect.sizeDelta = new Vector2(0f, NAV_HEIGHT);

            Canvas navCanvas = navRoot.GetComponent<Canvas>();
            navCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            navCanvas.overrideSorting = true;
            navCanvas.sortingOrder = 600;

            GameObject bg = CreateUIObject("Background", navRoot.transform);
            StretchFull(bg.GetComponent<RectTransform>());
            bg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.98f);

            GameObject row = CreateUIObject("Tabs", navRoot.transform);
            StretchFull(row.GetComponent<RectTransform>());

            Button shopButton = CreateTabButton(row.transform, "Shop", 0f, 1f / 3f, out Graphic shopSelected);
            Button camperButton = CreateTabButton(row.transform, "Camper", 1f / 3f, 2f / 3f, out Graphic camperSelected);
            Button profileButton = CreateTabButton(row.transform, "Profile", 2f / 3f, 1f, out Graphic profileSelected);

            AssignTabIcon(shopButton, "Assets/Project Files/Game/Images/ui_icon_store.png");
            AssignTabIcon(camperButton, "Assets/Project Files/Game/Images/ui_icon_map.png");

            UIBottomNavBar nav = navRoot.AddComponent<UIBottomNavBar>();
            SerializedObject so = new SerializedObject(nav);
            so.FindProperty("root").objectReferenceValue = navRoot;
            so.FindProperty("shopButton").objectReferenceValue = shopButton;
            so.FindProperty("camperButton").objectReferenceValue = camperButton;
            so.FindProperty("profileButton").objectReferenceValue = profileButton;
            so.FindProperty("shopSelected").objectReferenceValue = shopSelected;
            so.FindProperty("camperSelected").objectReferenceValue = camperSelected;
            so.FindProperty("profileSelected").objectReferenceValue = profileSelected;
            so.ApplyModifiedPropertiesWithoutUndo();

            navRoot.SetActive(false);
        }

        private static Button CreateTabButton(Transform parent, string label, float anchorMinX, float anchorMaxX, out Graphic selectedGraphic)
        {
            const float iconSize = BottomNavLayout.IconSize;

            GameObject tab = CreateUIObject(label, parent);
            RectTransform tabRect = tab.GetComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(anchorMinX, 0f);
            tabRect.anchorMax = new Vector2(anchorMaxX, 1f);
            tabRect.offsetMin = Vector2.zero;
            tabRect.offsetMax = Vector2.zero;
            tabRect.pivot = new Vector2(0.5f, 0.5f);

            Image hitArea = tab.AddComponent<Image>();
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;

            GameObject iconGo = CreateUIObject("Icon", tab.transform);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            // Selected background pill (transparent when inactive)
            GameObject selBgGo = CreateUIObject("Selected Bg", tab.transform);
            RectTransform selBgRect = selBgGo.GetComponent<RectTransform>();
            selBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            selBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            selBgRect.pivot = new Vector2(0.5f, 0.5f);
            selBgRect.anchoredPosition = new Vector2(0f, BottomNavLayout.IconYOffset);
            selBgRect.sizeDelta = new Vector2(BottomNavLayout.SelectedBgSize, BottomNavLayout.SelectedBgSize);
            Image selBg = selBgGo.AddComponent<Image>();
            selBg.color = new Color(0.2f, 0.55f, 0.35f, 0f);
            selBg.raycastTarget = false;
            selectedGraphic = selBg;

            iconRect.anchoredPosition = new Vector2(0f, BottomNavLayout.IconYOffset);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            Image icon = iconGo.AddComponent<Image>();
            icon.color = new Color(0.45f, 0.45f, 0.45f, 1f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Button button = tab.AddComponent<Button>();
            button.targetGraphic = icon;
            return button;
        }

        private static void AssignTabIcon(Button button, string spritePath)
        {
            if (button == null) return;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[BottomNav] 图标 sprite 未找到: {spritePath}");
                return;
            }

            Image icon = button.targetGraphic as Image;
            if (icon == null) return;

            icon.sprite = sprite;
            icon.color = Color.white;
        }

        private static Text CreateLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Text text = go.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

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
    }
}
#endif
