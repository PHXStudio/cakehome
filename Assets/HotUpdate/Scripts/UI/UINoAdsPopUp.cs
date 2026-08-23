using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UINoAdsPopUp : MonoBehaviour, IPopupWindow
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button bigCloseButton;
        [SerializeField] Button smallCloseButton;
        [SerializeField] IAPButton removeAdsButton;

        public bool IsOpened => gameObject.activeSelf;

        private UIFadeAnimation backFade;

        private void OnEnable()
        {
            IAPManager.PurchaseCompleted += OnPurchaseCompleted;
        }

        private void OnDisable()
        {
            IAPManager.PurchaseCompleted -= OnPurchaseCompleted;
        }

        public void Init()
        {
            backFade = new UIFadeAnimation(gameObject);

            bigCloseButton.onClick.AddListener(ClosePanel);
            smallCloseButton.onClick.AddListener(ClosePanel);

            IAPManager.SubscribeOnPurchaseModuleInitted(OnPurchaseModuleInitted);

            backFade.Hide(immediately: true);
            panelScalable.Hide(immediately: true);
        }

        private void OnPurchaseModuleInitted()
        {
            removeAdsButton.Init(ProductKeyType.NoAdsPack);
        }

        private void OnPurchaseCompleted(ProductKeyType productKeyType)
        {
            if(productKeyType == ProductKeyType.NoAdsPack)
            {
                AdsManager.DisableForcedAdForever();

                gameObject.SetActive(false);

            }
        }

        public void Show()
        {
            bigCloseButton.interactable = true;
            smallCloseButton.interactable = true;

            gameObject.SetActive(true);
            backFade.Show(0.2f, onCompleted: () =>
            {
                panelScalable.Show(immediately: false, duration: 0.3f);
            });

        }

        private void ClosePanel()
        {
            bigCloseButton.interactable = false;
            smallCloseButton.interactable = false;
            
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.4f, onCompleted: () =>
            {
                gameObject.SetActive(false);
            });

        }
    }
}