using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Fragment exchange popup: spend cake fragments (from tile-match drops and client orders)
    /// on merge-side resources. Fully runtime-built — no prefab required; shown via <see cref="Show"/>.
    /// Offers: 10 fragments → +25 energy; 20 fragments → random grade-1 spawner.
    /// </summary>
    public static class UIFragmentExchangePanel
    {
        private const int ENERGY_COST = 10;
        private const int ENERGY_AMOUNT = 25;
        private const int SPAWNER_COST = 20;
        private static readonly string[] SPAWNER_POOL = { "Kettle", "Mixer" };

        private static GameObject panelRoot;
        private static Text countText;
        private static Button energyButton;
        private static Button spawnerButton;

        public static void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                Refresh();
                return;
            }

            Build();
            Refresh();
        }

        private static void Build()
        {
            Canvas hostCanvas = null;
            UIController uiController = Object.FindObjectByType<UIController>();
            if (uiController != null)
                hostCanvas = uiController.GetComponent<Canvas>();

            panelRoot = new GameObject("Fragment Exchange Panel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            panelRoot.layer = 5;
            RectTransform rootRect = (RectTransform)panelRoot.transform;
            if (hostCanvas != null)
            {
                rootRect.SetParent(hostCanvas.transform, false);
            }
            else
            {
                Canvas fallback = panelRoot.GetComponent<Canvas>();
                fallback.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            Canvas canvas = panelRoot.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 800;

            StretchFull(rootRect);

            // Backdrop (click to close)
            GameObject backdrop = CreateUI("Backdrop", rootRect);
            StretchFull((RectTransform)backdrop.transform);
            Image backdropImage = backdrop.AddComponent<Image>();
            backdropImage.color = new Color(0f, 0f, 0f, 0.6f);
            Button backdropButton = backdrop.AddComponent<Button>();
            backdropButton.onClick.AddListener(Close);

            // Panel
            GameObject panel = CreateUI("Panel", rootRect);
            RectTransform panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 520f);
            panelRect.anchoredPosition = Vector2.zero;
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(1f, 0.98f, 0.95f, 1f);

            // Title
            Text title = CreateLabel(panel.transform, "Title", "碎片兑换", 44, TextAnchor.MiddleCenter);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 80f);
            titleRect.anchoredPosition = Vector2.zero;
            title.color = new Color(0.35f, 0.25f, 0.2f, 1f);

            // Count
            countText = CreateLabel(panel.transform, "Count", "", 34, TextAnchor.MiddleCenter);
            RectTransform countRect = (RectTransform)countText.transform;
            countRect.anchorMin = new Vector2(0f, 1f);
            countRect.anchorMax = new Vector2(1f, 1f);
            countRect.pivot = new Vector2(0.5f, 1f);
            countRect.sizeDelta = new Vector2(0f, 60f);
            countRect.anchoredPosition = new Vector2(0f, -80f);
            countText.color = new Color(0.55f, 0.35f, 0.2f, 1f);

            // Option 1: energy
            energyButton = CreateOptionButton(panel.transform, "Energy Option",
                $"{ENERGY_COST} 碎片 → +{ENERGY_AMOUNT} 能量", new Vector2(0f, -160f),
                () =>
                {
                    if (!FragmentController.Spend(ENERGY_COST)) return;
                    EnergyController.Add(ENERGY_AMOUNT, ignoreCap: true);
                    AudioController.PlaySound(AudioController.GetClip("energy_restore"));
                });

            // Option 2: random spawner
            spawnerButton = CreateOptionButton(panel.transform, "Spawner Option",
                $"{SPAWNER_COST} 碎片 → 随机 1 级生成器", new Vector2(0f, -260f),
                () =>
                {
                    if (!FragmentController.Spend(SPAWNER_COST)) return;
                    string typeId = SPAWNER_POOL[Random.Range(0, SPAWNER_POOL.Length)];
                    TaskController.Instance?.SpawnerQueue?.Push(typeId, 1);
                    AudioController.PlaySound(AudioController.GetClip("button_sound"));
                });

            // Close button
            Button closeButton = CreateOptionButton(panel.transform, "Close Button", "关闭",
                new Vector2(0f, -380f), Close);
            RectTransform closeRect = (RectTransform)closeButton.transform;
            closeRect.sizeDelta = new Vector2(300f, 80f);

            FragmentController.OnChanged += Refresh;
        }

        public static void Close()
        {
            FragmentController.OnChanged -= Refresh;

            if (panelRoot != null)
            {
                Object.Destroy(panelRoot);
                panelRoot = null;
                countText = null;
                energyButton = null;
                spawnerButton = null;
            }
        }

        private static void Refresh()
        {
            if (panelRoot == null) return;

            if (countText != null)
                countText.text = $"蛋糕碎片：{FragmentController.Count}";
            if (energyButton != null)
                energyButton.interactable = FragmentController.Has(ENERGY_COST);
            if (spawnerButton != null)
                spawnerButton.interactable = FragmentController.Has(SPAWNER_COST);
        }

        // ------------------------------------------------------------------ builders

        private static GameObject CreateUI(string name, Transform parent)
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
        }

        private static Font LoadChineseFont()
        {
            Font font = Resources.Load<Font>("ChineseFont");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        private static Text CreateLabel(Transform parent, string name, string content, int size, TextAnchor anchor)
        {
            GameObject go = CreateUI(name, parent);
            StretchFull((RectTransform)go.transform);
            Text text = go.AddComponent<Text>();
            text.font = LoadChineseFont();
            text.text = content;
            text.fontSize = size;
            text.alignment = anchor;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateOptionButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = CreateUI(name, parent);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(520f, 90f);
            rect.anchoredPosition = position;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.85f, 0.55f, 0.3f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            button.onClick.AddListener(Refresh);

            Text text = CreateLabel(go.transform, "Label", label, 30, TextAnchor.MiddleCenter);
            text.color = Color.white;

            return button;
        }
    }
}
