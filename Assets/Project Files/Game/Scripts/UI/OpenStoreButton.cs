using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;

namespace Watermelon
{
    [RequireComponent(typeof(Button))]
    public class OpenStoreButton : MonoBehaviour
    {
        // Tutorial-only gate — while locked, the header's store buttons (coins/gems add buttons)
        // are a no-op, so the onboarding flow can't be derailed by the player leaving to the store.
        private static bool isLocked;

        public static void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        private void Awake()
        {
            Button button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClicked);
        }

        public void OnButtonClicked()
        {
            if (isLocked) return;

            CakeUIBridge.OpenStore?.Invoke();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }
    }
}