using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIImageStepView : UIDialogStepView
    {
        [SerializeField] Image       image;
        [SerializeField] CanvasGroup canvasGroup;

        private TweenCase fadeTween;

        public override bool IsAnimating => fadeTween != null && !fadeTween.IsCompleted;

        public UIImageStepView Setup(ImageStep step, bool instant)
        {
            image.sprite = step.image;

            if (instant)
            {
                canvasGroup.alpha = 1;
            }
            else
            {
                canvasGroup.alpha = 0;
                fadeTween = canvasGroup.DOFade(1f, 0.25f, unscaledTime: true);
            }

            return this;
        }

        public override void CompleteInstant()
        {
            fadeTween.CompleteActive();
            canvasGroup.alpha = 1;
        }

        private void OnDestroy()
        {
            fadeTween.KillActive();
        }
    }
}
