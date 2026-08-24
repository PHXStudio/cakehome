using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Single scene-level overlay (sibling of MergeGrid's "Game Area") that repositions itself
    // on top of whichever object is selected, instead of every item carrying its own highlight.
    [RequireComponent(typeof(Image))]
    public class MergeFieldObjectSelection : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Image image;

        public static MergeFieldObjectSelection Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            rectTransform = (RectTransform)transform;
            image = GetComponent<Image>();

            // The GameObject itself stays active so this singleton initializes at scene load —
            // visibility is toggled via the Image instead.
            image.enabled = false;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public void Show(MergeFieldObject target)
        {
            RectTransform targetRect = (RectTransform)target.transform;

            rectTransform.position = targetRect.position;
            rectTransform.sizeDelta = targetRect.rect.size + new Vector2(10f, 10f);

            image.enabled = true;
            BounceAnimation.Bounce(rectTransform);
        }

        public void Hide()
        {
            image.enabled = false;
        }
    }
}
