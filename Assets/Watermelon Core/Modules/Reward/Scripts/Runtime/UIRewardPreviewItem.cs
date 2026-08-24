using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// MonoBehaviour that displays a single reward preview entry inside the rewards popup.
    /// Populated by <see cref="UIRewardsPopup"/> with an <see cref="IRewardPreview"/> at runtime.
    /// Subclass and override <see cref="OnInitialized"/> to add custom initialization logic.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIRewardPreviewItem : MonoBehaviour
    {
        [SerializeField] Image image;
        public Image Image => image;

        [SerializeField] TextMeshProUGUI text;
        public TextMeshProUGUI Text => text;

        private CanvasGroup canvasGroup;
        public CanvasGroup CanvasGroup => canvasGroup;

        private IRewardPreview rewardPreview;

        public void Init(IRewardPreview rewardPreview, Sprite defaultSprite)
        {
            this.rewardPreview = rewardPreview;

            canvasGroup = GetComponent<CanvasGroup>();

            if (image != null)
            {
                Sprite sprite = rewardPreview.Icon;
                if (sprite == null)
                    sprite = defaultSprite;

                image.sprite = sprite;
            }

            if (text != null)
                text.text = rewardPreview.Text;

            OnInitialized();
        }

        protected virtual void OnInitialized() { }
    }
}
