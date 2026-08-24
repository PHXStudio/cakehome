using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIThoughtStepView : UIDialogStepView
    {
        [SerializeField] Image    portrait;
        [SerializeField] Image    ringBack;
        [SerializeField] Image    ringFront;
        [SerializeField] Image    titleBackground;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] RectTransform characterTransform;
        [SerializeField] RectTransform messageBoxTransform;

        public UIThoughtStepView Setup(ThoughtStep step, bool instant)
        {
            portrait.sprite = step.character?.GetPortrait(step.emotion);

            Color color = step.character?.PortraitColor ?? Color.white;
            ringBack.color = color;
            ringFront.color = color;
            titleBackground.color = color;
            nameText.text = step.character?.CharacterName ?? string.Empty;

            bodyText.text = step.text;

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
    }
}
