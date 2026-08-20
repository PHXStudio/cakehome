using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>换装槽位单行：名称 + 状态（装备/已购/购买按钮）。</summary>
    public class AvatarSlotRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button buyButton;

        private AvatarController.AvatarSlot slot;

        public void Setup(AvatarController.AvatarSlot slotType)
        {
            slot = slotType;

            if (nameText != null)
                nameText.text = AvatarController.SlotItems[(int)slot];

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

            if (AvatarController.IsEquipped(slot))
            {
                if (statusText != null) statusText.text = "已装备";
                if (buyButton != null) buyButton.gameObject.SetActive(false);
            }
            else if (AvatarController.IsOwned(slot))
            {
                if (statusText != null) statusText.text = "已拥有";
                if (buyButton != null)
                {
                    buyButton.gameObject.SetActive(true);
                    var tmp = buyButton.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) tmp.text = "装备";
                    buyButton.interactable = true;
                }
            }
            else
            {
                if (statusText != null) statusText.text = $"{AvatarController.SlotCosts[(int)slot]} 积分";
                if (buyButton != null)
                {
                    buyButton.gameObject.SetActive(true);
                    var tmp = buyButton.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) tmp.text = "购买";
                    buyButton.interactable = CurrencyController.Get(CurrencyType.Coins) >= AvatarController.SlotCosts[(int)slot];
                }
            }
        }

        private void OnClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (AvatarController.IsOwned(slot))
                AvatarController.Equip(slot);
            else
                AvatarController.BuyAndEquip(slot);

            Refresh();
        }
    }
}
