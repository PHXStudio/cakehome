using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 配方面板单行：蛋糕名 + 碎片进度（x/y）+ 解封按钮。
    /// 已解封显示「2×」状态。
    /// </summary>
    public class RecipeRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] Button unlockButton;

        private RecipeDefinition recipe;

        public void Setup(RecipeDefinition definition)
        {
            recipe = definition;

            if (nameText != null)
                nameText.text = definition.DisplayName;

            if (unlockButton != null)
            {
                unlockButton.onClick.RemoveAllListeners();
                unlockButton.onClick.AddListener(OnUnlockClicked);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (recipe == null)
                return;

            if (RecipeController.IsUnlocked(recipe.Id))
            {
                if (nameText != null)
                    nameText.text = $"{recipe.DisplayName} 2×";
                if (progressText != null)
                    progressText.text = "已解封";
                if (unlockButton != null)
                    unlockButton.gameObject.SetActive(false);
            }
            else
            {
                if (nameText != null)
                    nameText.text = recipe.DisplayName;
                if (progressText != null)
                    progressText.text = $"{RecipeController.GetFragments(recipe.Id)}/{RecipeController.FragmentsToUnlock}";
                if (unlockButton != null)
                {
                    unlockButton.gameObject.SetActive(true);
                    unlockButton.interactable = RecipeController.CanUnlock(recipe.Id);
                }
            }
        }

        private void OnUnlockClicked()
        {
            if (recipe == null)
                return;

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (RecipeController.TryUnlock(recipe.Id))
            {
                Refresh();
                CustomAnalytics.TrackRecipeUnlock(recipe.Id);
            }
            else
            {
                // 碎片不够或积分不足
                Debug.Log($"[Recipe] 解封 {recipe.DisplayName} 失败：碎片 {RecipeController.GetFragments(recipe.Id)}/{RecipeController.FragmentsToUnlock}，积分 {CurrencyController.Get(CurrencyType.Coins)}");
            }
        }
    }
}
