using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIShopFreezerPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Transform content;
        [SerializeField] Button closeButton;
        [SerializeField] Text emptyLabel;

        private readonly System.Collections.Generic.List<GameObject> spawnedRows = new System.Collections.Generic.List<GameObject>();
        private bool wired;

        public void Configure(GameObject rootObject, Transform contentTransform, Button close, Text empty)
        {
            root = rootObject;
            content = contentTransform;
            closeButton = close;
            emptyLabel = empty;
            WireClose();
            Hide();
        }

        private void Awake()
        {
            WireClose();
            if (root == null)
                root = gameObject;
        }

        private void WireClose()
        {
            if (wired || closeButton == null)
                return;

            closeButton.onClick.AddListener(Hide);
            wired = true;
        }

        public void Show()
        {
            if (root != null)
                root.SetActive(true);

            Rebuild();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void Toggle()
        {
            if (root != null && root.activeSelf)
                Hide();
            else
                Show();
        }

        public void Rebuild()
        {
            ClearRows();

            if (!ShopController.IsInitialized || content == null)
                return;

            var freezer = ShopController.GetFreezerCakes();
            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(freezer.Count == 0);

            for (int i = 0; i < freezer.Count; i++)
            {
                OwnedCake cake = freezer[i];
                CakeDefinition def = ShopController.GetDefinition(cake);
                string label = def != null ? def.DisplayName : cake.DefinitionId;

                GameObject row = new GameObject($"FreezerRow_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(content, false);

                LayoutElement layout = row.AddComponent<LayoutElement>();
                layout.minHeight = 64f;
                layout.preferredHeight = 64f;

                Image bg = row.GetComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0.85f);

                GameObject textGo = new GameObject("Label", typeof(RectTransform));
                textGo.transform.SetParent(row.transform, false);
                RectTransform textRect = textGo.GetComponent<RectTransform>();
                Stretch(textRect);
                textRect.offsetMin = new Vector2(16f, 0f);
                textRect.offsetMax = new Vector2(-120f, 0f);

                Text text = textGo.AddComponent<Text>();
                BottomNavTextUtil.Apply(text, label, 28);
                text.alignment = TextAnchor.MiddleLeft;
                text.color = new Color(0.2f, 0.25f, 0.2f);

                GameObject placeGo = new GameObject("Place", typeof(RectTransform));
                placeGo.transform.SetParent(row.transform, false);
                RectTransform placeRect = placeGo.GetComponent<RectTransform>();
                placeRect.anchorMin = new Vector2(1f, 0.15f);
                placeRect.anchorMax = new Vector2(1f, 0.85f);
                placeRect.pivot = new Vector2(1f, 0.5f);
                placeRect.sizeDelta = new Vector2(110f, 0f);
                placeRect.anchoredPosition = new Vector2(-12f, 0f);

                Image placeBg = placeGo.AddComponent<Image>();
                placeBg.color = new Color(0.3f, 0.7f, 0.45f, 1f);
                Button placeButton = placeGo.AddComponent<Button>();

                GameObject placeLabelGo = new GameObject("Text", typeof(RectTransform));
                placeLabelGo.transform.SetParent(placeGo.transform, false);
                Stretch(placeLabelGo.GetComponent<RectTransform>());
                Text placeLabel = placeLabelGo.AddComponent<Text>();
                BottomNavTextUtil.Apply(placeLabel, "上架", 26);
                placeLabel.color = Color.white;

                string instanceId = cake.InstanceId;
                placeButton.onClick.AddListener(() =>
                {
                    int empty = ShopController.FindEmptyShelf();
                    if (empty >= 0)
                    {
                        ShopController.PlaceCake(instanceId, empty);
                        Rebuild();
                    }
                });

                spawnedRows.Add(row);
            }
        }

        private void ClearRows()
        {
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null)
                    Destroy(spawnedRows[i]);
            }

            spawnedRows.Clear();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
