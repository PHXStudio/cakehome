using TMPro;
using UnityEngine;

namespace Watermelon
{
    public abstract class FloatingTextBaseBehavior : MonoBehaviour
    {
        [SerializeField] protected TMP_Text textRef;

        public SimpleCallback OnAnimationCompleted;

        // Estimated rendered size of the given text in canvas units — used by the controller
        // to clamp spawn positions so the whole text stays on screen, not just its center.
        public Vector2 GetTextSize(string text, float scaleMultiplier)
        {
            if (textRef == null || string.IsNullOrEmpty(text)) return Vector2.zero;
            return textRef.GetPreferredValues(text) * scaleMultiplier;
        }

        public virtual void Activate(string text, float scaleMultiplier, Color color)
        {
            textRef.text = text;
            textRef.color = color;

            InvokeCompleteEvent();
        }

        protected void InvokeCompleteEvent()
        {
            OnAnimationCompleted?.Invoke();
        }
    }
}