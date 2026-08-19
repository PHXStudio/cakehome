using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 配方面板：展示 6 个配方的碎片进度 + 解封按钮（支付积分解封 → 门店 2×）。
    /// 结构由编辑器脚本构建，运行时只做数据刷新与按钮绑定。
    /// </summary>
    public class UIRecipePanel : MonoBehaviour
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button closeButton;
        [SerializeField] Transform contentRoot;
        [SerializeField] GameObject rowTemplate;

        private bool isOpened;

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
        }

        public void Hide()
        {
            isOpened = false;

            panelScalable.Hide(immediately: false, duration: 0.3f, onCompleted: () =>
            {
                gameObject.SetActive(false);
            });
        }

        private void RebuildRows()
        {
            if (contentRoot == null || rowTemplate == null)
                return;

            RecipeDefinition[] recipes = RecipeController.Recipes;
            if (recipes == null)
                return;

            // 幂等：清除上一次构建的行（保留模板）
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = contentRoot.GetChild(i);
                if (child.gameObject != rowTemplate)
                    DestroyImmediate(child.gameObject);
            }

            for (int i = 0; i < recipes.Length; i++)
            {
                GameObject row = Instantiate(rowTemplate, contentRoot);
                row.name = "Row " + i;
                row.SetActive(true);

                RecipeRow rowComp = row.GetComponent<RecipeRow>();
                if (rowComp == null)
                    rowComp = row.AddComponent<RecipeRow>();

                rowComp.Setup(recipes[i]);
            }
        }

        private void RefreshRows()
        {
            for (int i = 0; i < contentRoot.childCount; i++)
            {
                RecipeRow row = contentRoot.GetChild(i).GetComponent<RecipeRow>();
                if (row != null)
                    row.Refresh();
            }
        }
    }
}
