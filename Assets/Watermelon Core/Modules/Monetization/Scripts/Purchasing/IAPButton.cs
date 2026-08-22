using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class IAPButton : MonoBehaviour
    {
        [SerializeField] Image backImage;
        [SerializeField] Button button;
        [SerializeField] TMP_Text priceText;
        [SerializeField] GameObject loadingObject;

        [Space]
        [SerializeField] Sprite activeBackSprite;
        [SerializeField] Sprite unactiveBackSprite;

        private ProductKeyType key;

        private void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        public void Init(ProductKeyType key)
        {
            this.key = key;

            UpdateState();
        }

        public void UpdateState()
        {
            UpdateState(IAPManager.GetProductData(key));
        }

        public void UpdateState(ProductData product)
        {
            if (loadingObject == null || priceText == null || backImage == null)
            {
                Debug.LogWarning($"[IAPButton] UI references are not assigned. Skipping UpdateState. Key: {key}");

                return;
            }

            if (product != null)
            {
                loadingObject.SetActive(false);
                priceText.gameObject.SetActive(true);

                backImage.sprite = activeBackSprite;

                if (product.Price != 0.01m)
                {
                    priceText.text = product.GetLocalPrice();
                }
                else
                {
                    IAPItem iapItem = IAPManager.GetIAPItem(key);
                    if(iapItem != null)
                    {
                        priceText.text = $"USD {iapItem.DefaultUSDPrice}";
                    }
                    else
                    {
                        priceText.text = product.GetLocalPrice();
                    }
                }
            }
            else
            {
                SetDisabledState();
            }
        }

        private void SetDisabledState()
        {
            if (loadingObject != null)
                loadingObject.SetActive(true);

            if (priceText != null)
                priceText.gameObject.SetActive(false);

            if (backImage != null && unactiveBackSprite != null)
                backImage.sprite = unactiveBackSprite;
        }

        private void OnButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_HARD);
#endif

            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            IAPManager.BuyProduct(key);
        }
    }
}