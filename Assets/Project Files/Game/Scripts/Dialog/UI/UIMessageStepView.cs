using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIMessageStepView : UIDialogStepView
    {
        private const float OverflowPadding = 20f; // split between the gap above and below the text

        [SerializeField] Image portrait;
        [SerializeField] Image nameBackground;
        [SerializeField] Image ringOne;
        [SerializeField] Image ringTwo;
        [SerializeField] Image characterBackground;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] RectTransform characterTransform;
        [SerializeField] RectTransform messageBoxTransform;

        public UIMessageStepView Setup(MessageStep step, bool instant)
        {
            portrait.sprite = step.character?.GetPortrait(step.emotion);
            nameBackground.color = step.character?.PortraitColor ?? Color.white;
            ringOne.color = step.character?.PortraitColor ?? Color.white;
            ringTwo.color = step.character?.PortraitColor ?? Color.white;
            characterBackground.color = (step.character?.PortraitColor ?? Color.white) * 0.7f;
            nameText.text = step.character?.CharacterName ?? string.Empty;
            bodyText.text = step.text;

            ResizeToFitText();

            if (instant)
            {
                characterTransform.localScale = Vector3.one;
                messageBoxTransform.localScale = Vector3.one;
            }
            else
            {
                characterTransform.localScale = Vector3.one * 0.7f;
                characterTransform.DOScale(1f, 0.25f, unscaledTime: true).SetEasing(Ease.Type.BackOut);

                messageBoxTransform.localScale = Vector3.one * 0.9f;
                messageBoxTransform.DOScale(1f, 0.25f, delay: 0.08f, unscaledTime: true).SetEasing(Ease.Type.BackOut);
            }

            return this;
        }

        // Body text grows past its authored height for long content; message box and
        // root grow by the same amount so the header, padding and character icon stay untouched.
        private void ResizeToFitText()
        {
            RectTransform bodyRect = bodyText.rectTransform;

            float preferredHeight = bodyText.GetPreferredValues(bodyRect.rect.width, 0f).y;
            float overflow = preferredHeight - bodyRect.rect.height;

            if (overflow <= 0f)
                return;

            float sidePadding = OverflowPadding * 0.5f;

            // Text box grows to fit content, then gets nudged down to leave a gap above it too.
            GrowHeight(bodyRect, overflow);
            bodyRect.anchoredPosition -= new Vector2(0f, sidePadding);

            float delta = overflow + OverflowPadding;
            GrowHeight(messageBoxTransform, delta);
            GrowHeight((RectTransform)transform, delta);
        }

        // Grows a rect's height by exactly 'delta', regardless of whether it's anchored
        // to a fixed size or stretched to its parent, keeping its top edge in place.
        private static void GrowHeight(RectTransform rect, float delta)
        {
            float stretch = rect.anchorMax.y - rect.anchorMin.y;
            float sizeDeltaChange = delta * (1f - stretch);

            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + sizeDeltaChange);
            rect.anchoredPosition -= new Vector2(0f, (1f - rect.pivot.y) * sizeDeltaChange);
        }
    }
}
