using UnityEngine.EventSystems;

namespace Watermelon
{
    public class SettingsRestoreButton : SettingsButtonBase
    {
        public override void Init()
        {
#if MODULE_IAP
            gameObject.SetActive(IAPManager.IsInitialized);
#else
            gameObject.SetActive(false);
#endif
        }

        public override void OnClick()
        {
#if MODULE_IAP
            IAPManager.RestorePurchases();
#endif

            // Play button sound
            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }

        public override void Select()
        {
            IsSelected = true;

            Button.Select();

            EventSystem.current.SetSelectedGameObject(null); //clear any previous selection (best practice)
            EventSystem.current.SetSelectedGameObject(Button.gameObject, new BaseEventData(EventSystem.current));
        }

        public override void Deselect()
        {
            IsSelected = false;

            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}