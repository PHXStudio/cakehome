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
    /// 构建商店页面标准 UI：UIShopPage.prefab（嵌套烘焙积分面板、冰柜面板、扩建弹窗）+ 替换 Game.unity 场景旧对象。
    /// 幂等可重跑。参考 ShopSceneSetup / BottomNavSceneSetup 的编辑器脚本模式。
    /// </summary>
    public static class ShopUIBuilder
    {
        private const string PREFAB_PATH = "Assets/Project Files/Game/Prefabs/UI/Canvas/UIShopPage.prefab";
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string PAGE_NAME = "UI Shop Page";

        private const string CURRENCY_PANEL_PREFAB = "Assets/Project Files/Game/Prefabs/UI/Currency Panel Simple.prefab";
        private const string FONT_PATH = "Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50.asset";
        private const string NINE_SLICE = "Assets/Watermelon Core/Core Resources/Images/core_universal_back.png";
        private const string BTN_PURPLE = "Assets/Project Files/Game/Images/Base/General UI/btn_purple.png";
        private const string BTN_GRAY = "Assets/Project Files/Game/Images/Base/General UI/btn_gray.png";
        private const string BTN_GREEN = "Assets/Project Files/Game/Images/Base/General UI/btn_green.png";
        private const string BTN_CLOSE = "Assets/Project Files/Game/Images/Base/General UI/btn_close.png";
        private const string PANEL_DARK = "Assets/Project Files/Game/Images/Base/General UI/panel_dark.png";
        private const string PANEL = "Assets/Project Files/Game/Images/Base/General UI/panel.png";

        [MenuItem("Actions/Shop/Rebuild Shop Page UI")]
        public static void Rebuild()
        {
            // 1. Build prefab in a temp scene
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = BuildPage();
            BindPageReferences(root);

            // Defensive: root must stay at scale 1, or the whole page becomes invisible.
            // A stale UIScaleAnimation.Hide() can zero it before saving.
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

            // 2. Replace scene-authored page in Game.unity
            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
            ReplacePageInGameScene();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            // cleanup temp scene back to game scene
            EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            Debug.Log($"[ShopUI] UIShopPage.prefab rebuilt + Game.unity page replaced. {PREFAB_PATH}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH));
        }

        private static void ReplacePageInGameScene()
        {
            UIController uiController = Object.FindObjectOfType<UIController>();
            if (uiController == null)
            {
                Debug.LogError("[ShopUI] UIController not found in Game scene.");
                return;
            }

            Transform canvas = uiController.transform;

            // Remove ALL previous UIShopPage instances. The prefab asset is saved under
            // "UIShopPage" so scene instances get that name, not the temp-build name
            // PAGE_NAME ("UI Shop Page"). Matching only PAGE_NAME left stale duplicates
            // (each Rebuild added a new copy) -> N hidden canvases overlapping other UIs.
            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);
                if (child != null && (child.name == PAGE_NAME || child.name == "UIShopPage"))
                    Object.DestroyImmediate(child.gameObject);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[ShopUI] Prefab missing: {PREFAB_PATH}");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas) as GameObject;
            if (instance != null)
                StretchFull(instance.GetComponent<RectTransform>());
            Debug.Log("[ShopUI] Game.unity: replaced 'UI Shop Page' with prefab instance (root stretched).");
        }

        // ------------------------------------------------------------------ Build

        private static GameObject BuildPage()
        {
            GameObject page = new GameObject(PAGE_NAME, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            page.layer = 5;
            StretchFull(page.GetComponent<RectTransform>());
            Canvas pageCanvas = page.GetComponent<Canvas>();
            // Note: do NOT assign renderMode explicitly here. ScreenSpaceOverlay is the
            // default (renderMode = 0); explicitly assigning it makes Unity serialize the
            // prefab root's m_LocalScale as 0, hiding the whole page. (Unity quirk.)
            pageCanvas.overrideSorting = true;
            pageCanvas.sortingOrder = 50;

            UIShopPage component = page.AddComponent<UIShopPage>();

            // Safe Area (bottom nav padding)
            RectTransform safeArea = CreateUIObject("Safe Area", page.transform).GetComponent<RectTransform>();
            StretchFull(safeArea);
            safeArea.offsetMin = new Vector2(0f, BottomNavLayout.Height);

            // --- Currency Panel Simple (nested prefab instance) ---
            GameObject currencyPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CURRENCY_PANEL_PREFAB);
            GameObject coinsPanelGo = currencyPanelPrefab != null
                ? PrefabUtility.InstantiatePrefab(currencyPanelPrefab, safeArea) as GameObject
                : CreateUIObject("Currency Panel Simple", safeArea);
            RectTransform coinsPanelRect = coinsPanelGo.GetComponent<RectTransform>();
            coinsPanelRect.anchorMin = new Vector2(1f, 1f);
            coinsPanelRect.anchorMax = new Vector2(1f, 1f);
            coinsPanelRect.pivot = new Vector2(1f, 1f);
            coinsPanelRect.anchoredPosition = new Vector2(-30f, -30f);
            CurrencyUIPanelSimple coinsPanel = coinsPanelGo.GetComponent<CurrencyUIPanelSimple>();

            // --- Top chips ---
            GameObject themeChip = CreateChip(safeArea, "Theme Chip", new Vector2(0.02f, 0.88f), new Vector2(0.32f, 0.98f), "今日热门", 34, CandyColors.LemonLt, CandyColors.LemonDk);
            GameObject rateChip = CreateChip(safeArea, "Rate Chip", new Vector2(0.34f, 0.88f), new Vector2(0.56f, 0.98f), "0/时", 34, CandyColors.MintLt, CandyColors.MintDk);
            GameObject pendingChip = CreateChip(safeArea, "Pending Chip", new Vector2(0.58f, 0.88f), new Vector2(0.72f, 0.98f), "待收获 0", 30, CandyColors.PeachLt, CandyColors.PeachDk);
            GameObject freshnessChip = CreateChip(safeArea, "Freshness Chip", new Vector2(0.74f, 0.88f), new Vector2(0.98f, 0.98f), "新鲜 100%", 26, CandyColors.AppleLt, CandyColors.AppleDk);

            TMP_Text themeText = themeChip.transform.Find("Text").GetComponent<TMP_Text>();
            TMP_Text rateText = rateChip.transform.Find("Text").GetComponent<TMP_Text>();
            TMP_Text pendingText = pendingChip.transform.Find("Text").GetComponent<TMP_Text>();
            TMP_Text freshnessText = freshnessChip.transform.Find("Text").GetComponent<TMP_Text>();

            // --- Side buttons ---
            GameObject freezerBtn = CreateSideButton(safeArea, "FreezerButton", new Vector2(0.03f, 0.70f), new Vector2(0.20f, 0.84f), "冰柜", CandyColors.BlueberryPri, LoadSprite(BTN_PURPLE));
            GameObject harvestBtn = CreateSideButton(safeArea, "HarvestButton", new Vector2(0.80f, 0.70f), new Vector2(0.97f, 0.84f), "收获", CandyColors.StrawberryPri, LoadSprite(BTN_PURPLE));
            GameObject expandBtn = CreateSideButton(safeArea, "ExpandButton", new Vector2(0.03f, 0.16f), new Vector2(0.20f, 0.30f), "扩建", CandyColors.ApplePri, LoadSprite(BTN_GREEN));
            GameObject recipeBtn = CreateSideButton(safeArea, "RecipeButton", new Vector2(0.80f, 0.55f), new Vector2(0.97f, 0.69f), "配方", CandyColors.GrapePri, LoadSprite(BTN_PURPLE));
            GameObject ingredientBtn = CreateSideButton(safeArea, "IngredientButton", new Vector2(0.80f, 0.40f), new Vector2(0.97f, 0.54f), "原料", CandyColors.PeachPri, LoadSprite(BTN_PURPLE));

            // Expand cost chip (under expand button)
            GameObject expandCostChip = CreateChip(safeArea, "ExpandCost Chip", new Vector2(0.03f, 0.08f), new Vector2(0.20f, 0.16f), "100", 26, CandyColors.OrangeLt, CandyColors.OrangeDk);
            TMP_Text expandCostText = expandCostChip.transform.Find("Text").GetComponent<TMP_Text>();

            // --- Freezer Panel ---
            UIShopFreezerPanel freezerPanel = BuildFreezerPanel(safeArea);

            // --- Expand Confirm PopUp ---
            UIShopExpandConfirmPopUp expandConfirm = BuildExpandConfirmPopUp(page.transform);

            // --- Recipe Panel (M2) ---
            UIRecipePanel recipePanel = BuildRecipePanel(page.transform);

            // --- Ingredient Panel (原料采购) ---
            UIIngredientPanel ingredientPanel = BuildIngredientPanel(page.transform);

            // --- hudElements (entrance order) ---
            RectTransform[] hudElements = new RectTransform[]
            {
                coinsPanelRect,
                themeChip.GetComponent<RectTransform>(),
                rateChip.GetComponent<RectTransform>(),
                pendingChip.GetComponent<RectTransform>(),
                freshnessChip.GetComponent<RectTransform>(),
                freezerBtn.GetComponent<RectTransform>(),
                harvestBtn.GetComponent<RectTransform>(),
                expandBtn.GetComponent<RectTransform>(),
                recipeBtn.GetComponent<RectTransform>(),
                ingredientBtn.GetComponent<RectTransform>(),
                expandCostChip.GetComponent<RectTransform>(),
            };

            // Bind UIShopPage fields
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("safeAreaRectTransform").objectReferenceValue = safeArea;
            so.FindProperty("coinsPanel").objectReferenceValue = coinsPanel;
            so.FindProperty("themeText").objectReferenceValue = themeText;
            so.FindProperty("rateText").objectReferenceValue = rateText;
            so.FindProperty("pendingText").objectReferenceValue = pendingText;
            so.FindProperty("freshnessText").objectReferenceValue = freshnessText;
            so.FindProperty("expandCostText").objectReferenceValue = expandCostText;
            so.FindProperty("harvestButton").objectReferenceValue = harvestBtn.GetComponent<Button>();
            so.FindProperty("harvestButtonRect").objectReferenceValue = harvestBtn.GetComponent<RectTransform>();
            so.FindProperty("freezerButton").objectReferenceValue = freezerBtn.GetComponent<Button>();
            so.FindProperty("expandButton").objectReferenceValue = expandBtn.GetComponent<Button>();
            so.FindProperty("freezerPanel").objectReferenceValue = freezerPanel;
            so.FindProperty("expandConfirmPopUp").objectReferenceValue = expandConfirm;
            so.FindProperty("recipeButton").objectReferenceValue = recipeBtn.GetComponent<Button>();
            so.FindProperty("recipePanel").objectReferenceValue = recipePanel;
            so.FindProperty("ingredientButton").objectReferenceValue = ingredientBtn.GetComponent<Button>();
            so.FindProperty("ingredientPanel").objectReferenceValue = ingredientPanel;
            so.FindProperty("hudElements").arraySize = hudElements.Length;
            for (int i = 0; i < hudElements.Length; i++)
                so.FindProperty("hudElements").GetArrayElementAtIndex(i).objectReferenceValue = hudElements[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return page;
        }

        private static void BindPageReferences(GameObject root)
        {
            // No-op: all bindings happen inside BuildPage on the live component.
            // Kept as a seam in case a second pass is needed.
        }

        // ------------------------------------------------------------------ Freezer panel

        private static UIShopFreezerPanel BuildFreezerPanel(Transform parent)
        {
            GameObject panel = CreateUIObject("Freezer Panel", parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.34f);
            panelRect.anchorMax = new Vector2(0.44f, 0.68f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Panel Back
            Image panelBack = panel.AddComponent<Image>();
            panelBack.sprite = LoadSprite(PANEL_DARK);
            panelBack.type = Image.Type.Sliced;
            panelBack.pixelsPerUnitMultiplier = 6.37f;
            panelBack.color = Color.white;
            panelBack.raycastTarget = true;

            // Title
            TMP_Text title = CreateTMP(panel.transform, "Title", "冷藏冰柜", 48, Color.white, TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            // Close button
            Button closeBtn = CreateButton(panel.transform, "Close", LoadSprite(BTN_CLOSE), new Color(1f, 1f, 1f, 0.9f));
            SetRect(closeBtn.GetComponent<RectTransform>(), new Vector2(0.84f, 0.86f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);

            // Scroll View
            GameObject scrollGo = CreateUIObject("Scroll View", panel.transform);
            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            SetRect(scrollRect, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.78f), Vector2.zero, Vector2.zero);
            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            GameObject viewport = CreateUIObject("Viewport", scrollGo.transform);
            StretchFull(viewport.GetComponent<RectTransform>());
            RectMask2D mask = viewport.AddComponent<RectMask2D>();
            Image viewportBg = viewport.AddComponent<Image>();
            viewportBg.color = new Color(1f, 1f, 1f, 0.05f);

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            StretchFull(contentRect);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;

            // Row template
            GameObject rowTemplate = CreateUIObject("Row Template", content.transform);
            rowTemplate.SetActive(false);
            RectTransform rowRect = rowTemplate.GetComponent<RectTransform>();
            LayoutElement rowLayout = rowTemplate.AddComponent<LayoutElement>();
            rowLayout.minHeight = 72f;
            rowLayout.preferredHeight = 72f;
            Image rowBg = rowTemplate.AddComponent<Image>();
            rowBg.sprite = LoadSprite(NINE_SLICE);
            rowBg.type = Image.Type.Sliced;
            rowBg.pixelsPerUnitMultiplier = 2f;
            rowBg.color = new Color(1f, 1f, 1f, 0.9f);
            rowBg.raycastTarget = true;

            // Cake dot
            Image cakeDot = CreateSlicedImage(rowTemplate.transform, "CakeDot", LoadSprite(NINE_SLICE), CandyColors.StrawberryPri, false);
            SetRect(cakeDot.rectTransform, new Vector2(0.03f, 0.2f), new Vector2(0.12f, 0.8f), Vector2.zero, Vector2.zero);

            // Label
            TMP_Text rowLabel = CreateTMP(rowTemplate.transform, "Label", "蛋糕", 34, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Left);
            SetRect(rowLabel.rectTransform, new Vector2(0.15f, 0f), new Vector2(0.68f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

            // Place button
            GameObject placeGo = CreateUIObject("Place", rowTemplate.transform);
            RectTransform placeRect = placeGo.GetComponent<RectTransform>();
            placeRect.anchorMin = new Vector2(0.72f, 0.15f);
            placeRect.anchorMax = new Vector2(0.97f, 0.85f);
            placeRect.offsetMin = Vector2.zero;
            placeRect.offsetMax = Vector2.zero;
            Button placeBtn = CreateButtonVisual(placeGo, LoadSprite(BTN_GREEN), CandyColors.MintPri);
            TMP_Text placeLabel = CreateTMP(placeGo.transform, "Text", "上架", 30, Color.white, TextAlignmentOptions.Center);
            StretchFull(placeLabel.rectTransform);

            // Empty label
            TMP_Text emptyLabel = CreateTMP(panel.transform, "Empty Label", "冰柜是空的", 30, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.Center);
            SetRect(emptyLabel.rectTransform, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.7f), Vector2.zero, Vector2.zero);

            // Component
            UIShopFreezerPanel component = panel.AddComponent<UIShopFreezerPanel>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("root").objectReferenceValue = panel;
            so.FindProperty("content").objectReferenceValue = contentRect;
            so.FindProperty("rowTemplate").objectReferenceValue = rowRect;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("emptyLabel").objectReferenceValue = emptyLabel;
            so.FindProperty("panelScalable").FindPropertyRelative("transform").objectReferenceValue = panelRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
            return component;
        }

        // ------------------------------------------------------------------ Expand confirm popup

        private static UIShopExpandConfirmPopUp BuildExpandConfirmPopUp(Transform parent)
        {
            GameObject popup = CreateUIObject("Expand Confirm PopUp", parent);
            RectTransform popupRect = popup.GetComponent<RectTransform>();
            StretchFull(popupRect);
            CanvasGroup cg = popup.AddComponent<CanvasGroup>();

            // Background mask
            Image bg = popup.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            bg.raycastTarget = true;

            // Panel
            GameObject panel = CreateUIObject("Panel", popup.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 400f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = LoadSprite(PANEL);
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 2f;
            panelImg.color = Color.white;

            TMP_Text title = CreateTMP(panel.transform, "Title", "扩建展位", 48, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.75f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text costText = CreateTMP(panel.transform, "Cost Text", "花费 0 烘焙积分", 38, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(costText.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 0.65f), Vector2.zero, Vector2.zero);

            // Buttons
            GameObject cancelGo = CreateUIObject("Cancel", panel.transform);
            RectTransform cancelRect = cancelGo.GetComponent<RectTransform>();
            SetRect(cancelRect, new Vector2(0.08f, 0.12f), new Vector2(0.46f, 0.32f), Vector2.zero, Vector2.zero);
            Button cancelBtn = CreateButtonVisual(cancelGo, LoadSprite(BTN_GRAY), new Color(0.9f, 0.9f, 0.9f));
            TMP_Text cancelLabel = CreateTMP(cancelGo.transform, "Text", "取消", 34, new Color(0.3f, 0.3f, 0.3f), TextAlignmentOptions.Center);
            StretchFull(cancelLabel.rectTransform);

            GameObject confirmGo = CreateUIObject("Confirm", panel.transform);
            RectTransform confirmRect = confirmGo.GetComponent<RectTransform>();
            SetRect(confirmRect, new Vector2(0.54f, 0.12f), new Vector2(0.92f, 0.32f), Vector2.zero, Vector2.zero);
            Button confirmBtn = CreateButtonVisual(confirmGo, LoadSprite(BTN_PURPLE), CandyColors.StrawberryPri);
            TMP_Text confirmLabel = CreateTMP(confirmGo.transform, "Text", "确认", 34, Color.white, TextAlignmentOptions.Center);
            StretchFull(confirmLabel.rectTransform);

            UIShopExpandConfirmPopUp component = popup.AddComponent<UIShopExpandConfirmPopUp>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("panelScalable").FindPropertyRelative("transform").objectReferenceValue = panelRect;
            so.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
            so.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.ApplyModifiedPropertiesWithoutUndo();

            popup.SetActive(false);
            return component;
        }

        // ------------------------------------------------------------------ Recipe panel (M2)

        private static UIRecipePanel BuildRecipePanel(Transform parent)
        {
            GameObject panelGo = CreateUIObject("Recipe Panel", parent);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            StretchFull(panelRect);
            CanvasGroup cg = panelGo.AddComponent<CanvasGroup>();

            // Background mask
            Image bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            bg.raycastTarget = true;

            // Panel
            GameObject panel = CreateUIObject("Panel", panelGo.transform);
            RectTransform bodyRect = panel.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(680f, 520f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = LoadSprite(PANEL);
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 2f;
            panelImg.color = Color.white;

            TMP_Text title = CreateTMP(panel.transform, "Title", "配方图鉴", 48, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.86f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            // Close button (top-right corner of body)
            GameObject closeGo = CreateUIObject("Close", panel.transform);
            RectTransform closeRect = closeGo.GetComponent<RectTransform>();
            SetRect(closeRect, new Vector2(0.88f, 0.84f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);
            Button closeBtn = CreateButton(closeGo.transform, "Close", LoadSprite(BTN_CLOSE), new Color(1f, 1f, 1f, 0.9f));

            // Scroll content root
            GameObject contentRoot = CreateUIObject("Content Root", panel.transform);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            SetRect(contentRect, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup vlg = contentRoot.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = contentRoot.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Row template (hidden)
            GameObject row = CreateUIObject("Row Template", contentRoot.transform);
            row.SetActive(false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 52f);
            Image rowImg = row.AddComponent<Image>();
            rowImg.sprite = LoadSprite(NINE_SLICE);
            rowImg.type = Image.Type.Sliced;
            rowImg.pixelsPerUnitMultiplier = 2f;
            rowImg.color = new Color(1f, 1f, 1f, 0.35f);

            TMP_Text rowName = CreateTMP(row.transform, "Name Text", "", 28, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Left);
            SetRect(rowName.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text rowProgress = CreateTMP(row.transform, "Progress Text", "", 26, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(rowProgress.rectTransform, new Vector2(0.55f, 0f), new Vector2(0.76f, 1f), Vector2.zero, Vector2.zero);

            GameObject unlockGo = CreateUIObject("Unlock", row.transform);
            RectTransform unlockRect = unlockGo.GetComponent<RectTransform>();
            SetRect(unlockRect, new Vector2(0.78f, 0.14f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);
            Button unlockBtn = CreateButtonVisual(unlockGo, LoadSprite(BTN_PURPLE), CandyColors.StrawberryPri);
            TMP_Text unlockLabel = CreateTMP(unlockGo.transform, "Text", "解封", 26, Color.white, TextAlignmentOptions.Center);
            StretchFull(unlockLabel.rectTransform);

            // Row 组件绑定（实例继承模板序列化引用）
            RecipeRow rowComponent = row.AddComponent<RecipeRow>();
            SerializedObject rowSo = new SerializedObject(rowComponent);
            rowSo.FindProperty("nameText").objectReferenceValue = rowName;
            rowSo.FindProperty("progressText").objectReferenceValue = rowProgress;
            rowSo.FindProperty("unlockButton").objectReferenceValue = unlockBtn;
            rowSo.ApplyModifiedPropertiesWithoutUndo();

            UIRecipePanel component = panelGo.AddComponent<UIRecipePanel>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("panelScalable").FindPropertyRelative("transform").objectReferenceValue = bodyRect;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("contentRoot").objectReferenceValue = contentRect;
            so.FindProperty("rowTemplate").objectReferenceValue = row;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelGo.SetActive(false);
            return component;
        }

        // ------------------------------------------------------------------ Ingredient panel (原料采购)

        private static UIIngredientPanel BuildIngredientPanel(Transform parent)
        {
            GameObject panelGo = CreateUIObject("Ingredient Panel", parent);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            StretchFull(panelRect);
            CanvasGroup cg = panelGo.AddComponent<CanvasGroup>();

            Image bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            bg.raycastTarget = true;

            GameObject panel = CreateUIObject("Panel", panelGo.transform);
            RectTransform bodyRect = panel.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(680f, 420f);
            Image panelImg = panel.AddComponent<Image>();
            panelImg.sprite = LoadSprite(PANEL);
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 2f;
            panelImg.color = Color.white;

            TMP_Text title = CreateTMP(panel.transform, "Title", "原料采购", 48, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Center);
            SetRect(title.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text totalText = CreateTMP(panel.transform, "Total Text", "当前产出 ×1", 30, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(totalText.rectTransform, new Vector2(0f, 0.7f), new Vector2(1f, 0.84f), Vector2.zero, Vector2.zero);

            GameObject closeGo = CreateUIObject("Close", panel.transform);
            RectTransform closeRect = closeGo.GetComponent<RectTransform>();
            SetRect(closeRect, new Vector2(0.88f, 0.84f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);
            Button closeBtn = CreateButton(closeGo.transform, "Close", LoadSprite(BTN_CLOSE), new Color(1f, 1f, 1f, 0.9f));

            Transform contentRoot = CreateUIObject("Content Root", panel.transform).transform;
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
            SetRect(contentRect, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.7f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup vlg = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            GameObject row = CreateUIObject("Row Template", contentRoot);
            row.SetActive(false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 52f);
            Image rowImg = row.AddComponent<Image>();
            rowImg.sprite = LoadSprite(NINE_SLICE);
            rowImg.type = Image.Type.Sliced;
            rowImg.pixelsPerUnitMultiplier = 2f;
            rowImg.color = new Color(1f, 1f, 1f, 0.35f);

            TMP_Text rowName = CreateTMP(row.transform, "Name Text", "", 28, new Color(0.2f, 0.25f, 0.2f), TextAlignmentOptions.Left);
            SetRect(rowName.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

            TMP_Text rowStatus = CreateTMP(row.transform, "Status Text", "", 26, new Color(0.3f, 0.35f, 0.3f), TextAlignmentOptions.Center);
            SetRect(rowStatus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.74f, 1f), Vector2.zero, Vector2.zero);

            GameObject buyGo = CreateUIObject("Buy", row.transform);
            RectTransform buyRect = buyGo.GetComponent<RectTransform>();
            SetRect(buyRect, new Vector2(0.76f, 0.14f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);
            Button buyBtn = CreateButtonVisual(buyGo, LoadSprite(BTN_PURPLE), CandyColors.PeachPri);
            TMP_Text buyLabel = CreateTMP(buyGo.transform, "Text", "购买", 26, Color.white, TextAlignmentOptions.Center);
            StretchFull(buyLabel.rectTransform);

            // Row 组件绑定
            IngredientRow rowComp = row.AddComponent<IngredientRow>();
            SerializedObject rowSo = new SerializedObject(rowComp);
            rowSo.FindProperty("nameText").objectReferenceValue = rowName;
            rowSo.FindProperty("statusText").objectReferenceValue = rowStatus;
            rowSo.FindProperty("buyButton").objectReferenceValue = buyBtn;
            rowSo.ApplyModifiedPropertiesWithoutUndo();

            UIIngredientPanel component = panelGo.AddComponent<UIIngredientPanel>();
            SerializedObject so = new SerializedObject(component);
            so.FindProperty("panelScalable").FindPropertyRelative("transform").objectReferenceValue = bodyRect;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("contentRoot").objectReferenceValue = contentRoot;
            so.FindProperty("rowTemplate").objectReferenceValue = row;
            so.FindProperty("totalText").objectReferenceValue = totalText;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelGo.SetActive(false);
            return component;
        }

        // ------------------------------------------------------------------ UI helpers

        private static GameObject CreateChip(Transform parent, string name, Vector2 min, Vector2 max, string value, int size, Color bg, Color textColor)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite(NINE_SLICE);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.color = new Color(bg.r, bg.g, bg.b, 0.92f);
            img.raycastTarget = false;

            TMP_Text text = CreateTMP(go.transform, "Text", value, size, textColor, TextAlignmentOptions.Center);
            StretchFull(text.rectTransform);
            return go;
        }

        private static GameObject CreateSideButton(Transform parent, string name, Vector2 min, Vector2 max, string label, Color color, Sprite sprite)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Button btn = CreateButtonVisual(go, sprite, color);

            TMP_Text text = CreateTMP(go.transform, "Text", label, 34, Color.white, TextAlignmentOptions.Center);
            StretchFull(text.rectTransform);
            return go;
        }

        private static Button CreateButton(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            return CreateButtonVisual(go, sprite, color);
        }

        private static Button CreateButtonVisual(GameObject go, Sprite sprite, Color color)
        {
            Image img = go.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2f;
            }
            img.color = color;
            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        private static Image CreateSlicedImage(Transform parent, string name, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject go = CreateUIObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return img;
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
