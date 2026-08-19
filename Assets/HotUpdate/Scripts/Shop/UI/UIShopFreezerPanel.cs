using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 冷藏冰柜面板：行增量复用（杜绝全量 Destroy/Instantiate）、标准九宫格样式、弹入动画。
    /// 结构由 ShopUIBuilder 构建并绑定引用。
    /// </summary>
    public class UIShopFreezerPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] RectTransform content;
        [SerializeField] RectTransform rowTemplate;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text emptyLabel;
        [SerializeField] UIScaleAnimation panelScalable;

        private sealed class FreezerRow
        {
            public RectTransform Root;
            public TMP_Text Label;
            public Button PlaceButton;
            public Image CakeDot;
            public string InstanceId;
        }

        private readonly List<FreezerRow> rows = new List<FreezerRow>();
        private bool wired;

        public bool IsOpen => root != null && root.activeSelf;

        public void Init()
        {
            WireClose();
            Hide(immediately: true);
        }

        private void WireClose()
        {
            if (wired || closeButton == null)
                return;

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Hide());
            wired = true;
        }

        public void Show()
        {
            if (root == null)
                return;

            root.SetActive(true);
            Rebuild();

            if (panelScalable != null)
                panelScalable.Show(immediately: false, duration: 0.3f);
        }

        public void Hide(bool immediately = false)
        {
            if (root == null)
                return;

            if (immediately)
            {
                root.SetActive(false);
                return;
            }

            if (panelScalable != null)
            {
                panelScalable.Hide(immediately: false, duration: 0.3f, onCompleted: () => root.SetActive(false));
            }
            else
            {
                root.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsOpen)
                Hide();
            else
                Show();
        }

        /// <summary>仅在面板打开时刷新（事件驱动，避免关闭状态空转）。</summary>
        public void RefreshIfOpen()
        {
            if (IsOpen)
                Rebuild();
        }

        public void Rebuild()
        {
            if (content == null)
                return;

            List<OwnedCake> freezer = ShopController.IsInitialized
                ? ShopController.GetFreezerCakes()
                : new List<OwnedCake>();

            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(freezer.Count == 0);

            int i = 0;
            for (; i < freezer.Count; i++)
            {
                FreezerRow row = GetOrCreateRow(i);

                OwnedCake cake = freezer[i];
                CakeDefinition def = ShopController.GetDefinition(cake);

                row.InstanceId = cake.InstanceId;
                row.Label.text = def != null ? def.DisplayName : cake.DefinitionId;

                if (row.CakeDot != null && def != null)
                    row.CakeDot.color = def.DisplayColor;

                row.Root.gameObject.SetActive(true);
            }

            for (; i < rows.Count; i++)
                rows[i].Root.gameObject.SetActive(false);
        }

        private FreezerRow GetOrCreateRow(int index)
        {
            if (index < rows.Count)
                return rows[index];

            RectTransform rowRect = Instantiate(rowTemplate, content);
            rowRect.gameObject.SetActive(true);

            FreezerRow row = new FreezerRow
            {
                Root = rowRect,
                Label = rowRect.Find("Label")?.GetComponent<TMP_Text>(),
                PlaceButton = rowRect.Find("Place")?.GetComponent<Button>(),
                CakeDot = rowRect.Find("CakeDot")?.GetComponent<Image>()
            };

            row.PlaceButton.onClick.RemoveAllListeners();
            row.PlaceButton.onClick.AddListener(() => OnPlaceClicked(row));

            rows.Add(row);
            return row;
        }

        private void OnPlaceClicked(FreezerRow row)
        {
            if (string.IsNullOrEmpty(row.InstanceId))
                return;

            int empty = ShopController.FindEmptyShelf();
            if (empty >= 0)
            {
                ShopController.PlaceCake(row.InstanceId, empty);
                Rebuild();
            }
        }
    }
}
