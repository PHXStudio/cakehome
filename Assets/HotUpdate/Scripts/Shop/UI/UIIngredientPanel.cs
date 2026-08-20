using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 原料采购面板：面粉/车厘子/彩虹糖针，积分购买获得临时产出增益。
    /// 结构由 ShopUIBuilder 构建，运行时刷新状态与倒计时。
    /// </summary>
    public class UIIngredientPanel : MonoBehaviour
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button closeButton;
        [SerializeField] Transform contentRoot;
        [SerializeField] GameObject rowTemplate;
        [SerializeField] TMP_Text totalText;

        private bool isOpened;
        private Coroutine refreshCoroutine;

        public bool IsOpened => isOpened;

        public void Init()
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);

            RebuildRows();

            panelScalable.Hide(immediately: true);
            isOpened = false;
        }

        public void Toggle()
        {
            if (isOpened)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            isOpened = true;
            gameObject.SetActive(true);

            RefreshRows();

            panelScalable.Show(immediately: false, duration: 0.3f);
            if (refreshCoroutine != null)
                StopCoroutine(refreshCoroutine);
            refreshCoroutine = StartCoroutine(RefreshCoroutine());
        }

        public void Hide()
        {
            isOpened = false;
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }

            panelScalable.Hide(immediately: false, duration: 0.3f, onCompleted: () =>
            {
                gameObject.SetActive(false);
            });
        }

        private System.Collections.IEnumerator RefreshCoroutine()
        {
            while (isOpened)
            {
                yield return new UnityEngine.WaitForSeconds(1f);
                RefreshRows();
            }
        }

        private void RebuildRows()
        {
            if (contentRoot == null || rowTemplate == null)
                return;

            // 幂等：清除上一次构建的行（保留模板）
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = contentRoot.GetChild(i);
                if (child.gameObject != rowTemplate)
                    DestroyImmediate(child.gameObject);
            }

            int count = IngredientController.Names.Length;
            for (int i = 0; i < count; i++)
            {
                GameObject row = Instantiate(rowTemplate, contentRoot);
                row.name = "Ingredient Row " + i;
                row.SetActive(true);

                IngredientRow rowComp = row.GetComponent<IngredientRow>();
                if (rowComp == null)
                    rowComp = row.AddComponent<IngredientRow>();

                rowComp.Setup((IngredientController.IngredientType)i);
            }

            RefreshTotal();
        }

        private void RefreshRows()
        {
            if (contentRoot == null)
                return;

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                IngredientRow row = contentRoot.GetChild(i).GetComponent<IngredientRow>();
                if (row != null)
                    row.Refresh();
            }

            RefreshTotal();
        }

        private void RefreshTotal()
        {
            if (totalText != null)
                totalText.text = $"当前产出 ×{IngredientController.GetTotalMultiplier():0.#}";
        }
    }
}
