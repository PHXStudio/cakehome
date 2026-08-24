using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Watermelon.FirstStartTutorial;

namespace Watermelon
{
    // Small standalone speech-bubble widget (portrait + one line of text) used by FirstStartTutorial
    // to narrate a single hint at a time — unlike UIDialog (Assets/Project Files/Game/Scripts/Dialog),
    // this isn't a queued multi-step scrollable panel, just an anchored on/off overlay bit.
    // Lives in the Game assembly (not Modules/Tutorial) because it references CharacterData/
    // EmotionType, which are Assembly-CSharp game types.
    public class UITutorialMessageBubble : MonoBehaviour, ITutorialCanvasElement
    {
        [SerializeField] Image portrait;
        [SerializeField] Image ringOne;
        [SerializeField] Image ringTwo;
        [SerializeField] Image characterBackground;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] RectTransform rectTransform;
        [SerializeField] RectTransform characterTransform;
        [SerializeField] RectTransform messageBoxTransform;

        private static UITutorialMessageBubble instance;

        private Vector2 defaultAnchoredPosition;
        private TweenCase characterTweenCase;
        private TweenCase messageBoxTweenCase;

        // Called by TutorialCanvasController.Init — the bubble starts inactive in the
        // scene, so Awake would never run and instance would stay null.
        public void Init()
        {
            instance = this;
            defaultAnchoredPosition = rectTransform.anchoredPosition;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // anchorBelow: when set, the bubble is repositioned (Y only) to sit just under that
        // element instead of its default scene position — e.g. a highlighted task-panel card
        // near the top of the screen, which would otherwise leave a big gap to the bubble's
        // usual bottom-of-screen spot. Both share the bubble's parent (Tutorial Overlay) as the
        // coordinate frame, same trick TutorialBoundsHelper uses for the spotlight mask.
        // fixedOffsetY: when set, overrides both anchorBelow and the default position with an
        // explicit anchoredPosition.y — for steps where neither the default spot nor an
        // anchor-below placement clears whatever's on screen (e.g. a bottom nav button).
        public static void Show(CharacterData character, EmotionType emotion, string text, RectTransform anchorBelow = null, float gapBelow = 90f, float? fixedOffsetY = null)
        {
            if (instance == null) return;

            Vector2 targetPosition = fixedOffsetY.HasValue
                ? new Vector2(instance.defaultAnchoredPosition.x, fixedOffsetY.Value)
                : anchorBelow != null
                    ? instance.ComputePositionBelow(anchorBelow, gapBelow)
                    : instance.defaultAnchoredPosition;

            // If the bubble is already on screen at the same spot, this is just a text update
            // (e.g. next hint in the same spot) — bounce only the message box, not the portrait
            // ring/character icon next to it, which hasn't changed.
            bool bounceMessageBoxOnly = instance.gameObject.activeSelf && instance.rectTransform.anchoredPosition == targetPosition;

            instance.gameObject.SetActive(true);
            instance.portrait.sprite = character != null ? character.GetPortrait(emotion) : null;
            instance.ringOne.color = character?.PortraitColor ?? Color.white;
            instance.ringTwo.color = character?.PortraitColor ?? Color.white;
            instance.characterBackground.color = (character?.PortraitColor ?? Color.white) * 0.7f;
            instance.bodyText.text = text;
            instance.rectTransform.anchoredPosition = targetPosition;
            instance.rectTransform.SetAsLastSibling();

            instance.characterTweenCase.KillActive();
            instance.messageBoxTweenCase.KillActive();

            if (bounceMessageBoxOnly)
            {
                instance.messageBoxTransform.localScale = Vector3.one * 0.75f;
                instance.messageBoxTweenCase = instance.messageBoxTransform.DOScale(1f, 0.3f, unscaledTime: true).SetEasing(Ease.Type.BackOut);
            }
            else
            {
                instance.characterTransform.localScale = Vector3.one * 0.5f;
                instance.characterTweenCase = instance.characterTransform.DOScale(1f, 0.3f, unscaledTime: true).SetEasing(Ease.Type.BackOut);

                instance.messageBoxTransform.localScale = Vector3.one * 0.65f;
                instance.messageBoxTweenCase = instance.messageBoxTransform.DOScale(1f, 0.3f, delay: 0.08f, unscaledTime: true).SetEasing(Ease.Type.BackOut);
            }
        }

        private Vector2 ComputePositionBelow(RectTransform target, float gap)
        {
            RectTransform parent = (RectTransform)rectTransform.parent;
            Rect targetRectInParent = TutorialBoundsHelper.ComputeEncapsulatingRect(new[] { target }, parent);

            float topEdgeY = targetRectInParent.yMin - gap;
            float bottomEdgeY = topEdgeY - rectTransform.rect.height;

            return new Vector2(defaultAnchoredPosition.x, bottomEdgeY - parent.rect.yMin);
        }

        public static void Hide()
        {
            if (instance == null) return;

            instance.characterTweenCase.KillActive();
            instance.messageBoxTweenCase.KillActive();
            instance.gameObject.SetActive(false);
        }
    }
}
