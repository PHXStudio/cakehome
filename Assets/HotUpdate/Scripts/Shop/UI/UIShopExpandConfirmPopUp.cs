using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 扩建展位确认弹窗：展示花费 + 确认/取消，避免裸扣烘焙积分。
    /// 由 ShopUIBuilder 构建面板结构并绑定引用。
    /// </summary>
    public class UIShopExpandConfirmPopUp : MonoBehaviour, IPopupWindow
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;
        [SerializeField] TMP_Text costText;

        private UIFadeAnimation backFade;
        private SimpleCallback onConfirm;

        public bool IsOpened => gameObject.activeSelf;

        public void Init()
        {
            backFade = new UIFadeAnimation(gameObject);

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(ClosePanel);

            backFade.Hide(immediately: true);
            if (panelScalable != null)
                panelScalable.Hide(immediately: true);
        }

        public void Show(int cost, SimpleCallback onConfirmCallback)
        {
            onConfirm = onConfirmCallback;

            if (costText != null)
                costText.text = $"花费 {cost} 烘焙积分";

            gameObject.SetActive(true);

            backFade.Show(0.2f, onCompleted: () =>
            {
                if (panelScalable != null)
                    panelScalable.Show(immediately: false, duration: 0.3f);
            });

        }

        private void OnConfirmClicked()
        {
            ClosePanel();
            onConfirm?.Invoke();
        }

        private void ClosePanel()
        {
            backFade.Hide(0.2f);

            if (panelScalable != null)
            {
                panelScalable.Hide(immediately: false, duration: 0.3f, onCompleted: () =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }

        }
    }
}
