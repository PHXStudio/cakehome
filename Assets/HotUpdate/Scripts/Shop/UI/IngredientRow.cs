using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>原料采购单行：名称 + 效果/倒计时 + 购买按钮。</summary>
    public class IngredientRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button buyButton;

        private IngredientController.IngredientType type;

        public void Setup(IngredientController.IngredientType ingredientType)
        {
            type = ingredientType;

            if (nameText != null)
                nameText.text = $"{IngredientController.Names[(int)type]} ×{IngredientController.Multipliers[(int)type]:0.#}";

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnClicked);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (nameText == null)
                return;

            if (IngredientController.IsActive(type))
            {
                double remain = IngredientController.GetRemainingSeconds(type);
                if (statusText != null)
                    statusText.text = $"生效 {FormatTime(remain)}";
                if (buyButton != null)
                {
                    buyButton.gameObject.SetActive(true);
                    var tmp = buyButton.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) tmp.text = "续购";
                    buyButton.interactable = CurrencyController.Get(CurrencyType.Coins) >= IngredientController.Costs[(int)type];
                }
            }
            else
            {
                if (statusText != null)
                    statusText.text = $"{IngredientController.Costs[(int)type]} 积分";
                if (buyButton != null)
                {
                    buyButton.gameObject.SetActive(true);
                    var tmp = buyButton.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) tmp.text = "购买";
                    buyButton.interactable = CurrencyController.Get(CurrencyType.Coins) >= IngredientController.Costs[(int)type];
                }
            }
        }

        private void OnClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (IngredientController.Buy(type))
                Refresh();
        }

        private static string FormatTime(double seconds)
        {
            int total = Mathf.CeilToInt((float)seconds);
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm}:{ss:00}";
        }
    }
}
