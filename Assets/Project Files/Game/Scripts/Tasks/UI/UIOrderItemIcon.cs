using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // One required-item slot inside a Client Order card: item icon + checkmark overlay.
    public class UIOrderItemIcon : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Image checkmark;
        [SerializeField] Image background;
        [SerializeField] Color defaultBackgroundColor = new Color(0.745283f, 0.4010268f, 0.15819688f, 0.3f);
        [SerializeField] Color activatedBackgroundColor = new Color(0.616f, 0.896f, 0.114f, 0.5f);

        public void SetState(Sprite sprite, bool collected)
        {
            icon.sprite = sprite;
            checkmark.enabled = collected;
            background.color = collected ? activatedBackgroundColor : defaultBackgroundColor;
        }
    }
}
