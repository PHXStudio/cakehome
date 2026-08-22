using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public abstract class MergeFieldObject : MonoBehaviour, ISelectableObject
    {
        [SerializeField] protected Image mainRenderer;
        [SerializeField] protected Image shadowRenderer;
        [SerializeField] protected Image lockedOverlay;
        [SerializeField] protected Image lockedOverlayBack;
        [SerializeField] protected Image lockedOverlayMask;

        public string TypeId { get; private set; }
        public int Grade { get; private set; }
        public bool IsHalfLocked { get; private set; }

        protected MergeItemData itemData;
        private TweenCase taskBounceTween;

        private Vector3 shadowBasePosition;

        public MergeItemData ItemData => itemData;
        public MergeGradeData GradeData => itemData?.GetGradeData(Grade);
        public Sprite CurrentSprite => mainRenderer != null ? mainRenderer.sprite : null;

        // False while mid-drag-pickup or mid-displacement-flight (see SetVisualVisible) — used by
        // ClientOrderHighlightController to avoid lighting up a cell before its occupant has
        // actually, visually arrived there.
        public bool IsVisualVisible => mainRenderer == null || mainRenderer.enabled;

        protected static readonly Color LOCKED_COLOR = new Color(0.8f, 0.8f, 0.8f, 1f);

        // MergeCellBackground owns all pointer/drag input now — items must never be raycast targets
        // (cells and items are siblings, so a raycastable item would swallow clicks meant for its cell).
        protected virtual void Awake()
        {
            if (mainRenderer != null)
                mainRenderer.raycastTarget = false;

            if (lockedOverlay != null)
                lockedOverlay.raycastTarget = false;

            if (lockedOverlayBack != null)
                lockedOverlayBack.raycastTarget = false;

            if (lockedOverlayMask != null)
                lockedOverlayMask.raycastTarget = false;

            if (shadowRenderer != null)
                shadowBasePosition = shadowRenderer.rectTransform.anchoredPosition3D;
        }

        public virtual void Init(MergeItemData data, int grade)
        {
            itemData = data;
            TypeId = data.TypeId;
            Grade = grade;
            SetHalfLocked(false);
            UpdateVisual();
        }

        // Drag-pickup visibility toggle — distinct from OnSelected/OnDeselected (click-selection state).
        public void SetVisualVisible(bool visible)
        {
            OnVisualsStateChanged(visible);
        }

        protected virtual void OnVisualsStateChanged(bool visible)
        {
            if (mainRenderer != null)
                mainRenderer.enabled = visible;

            // Hidden together with mainRenderer whenever a MergeFlyingIcon takes over (drag,
            // spawn flight, displacement) — the flying icon carries its own shadow (see
            // CopyShadowVisualTo) so this one doesn't sit behind, static, at the old spot.
            if (shadowRenderer != null)
                shadowRenderer.enabled = visible;
        }

        // Lets MergeFlyingIcon mirror this item's current shadow (sprite/offset/scale, including
        // any CustomShadow override) while it flies in place of the real item.
        public void CopyShadowVisualTo(Image target)
        {
            if (target == null)
                return;

            if (shadowRenderer == null)
            {
                target.enabled = false;
                return;
            }

            target.enabled = true;
            target.sprite = shadowRenderer.sprite;
            target.color = shadowRenderer.color;

            RectTransform targetRect = (RectTransform)target.transform;
            RectTransform sourceRect = shadowRenderer.rectTransform;
            targetRect.anchoredPosition3D = sourceRect.anchoredPosition3D;
            targetRect.sizeDelta = sourceRect.sizeDelta;
            targetRect.localScale = sourceRect.localScale;
        }

        public void SetHalfLocked(bool locked)
        {
            IsHalfLocked = locked;

            if (lockedOverlay != null)
                lockedOverlay.enabled = locked;

            if (lockedOverlayBack != null)
                lockedOverlayBack.enabled = locked;

            if (lockedOverlayMask != null)
                lockedOverlayMask.enabled = locked;

            OnHalfLockedStateChanged(locked);
        }

        protected virtual void OnHalfLockedStateChanged(bool locked)
        {
            if (mainRenderer != null)
                mainRenderer.color = locked ? LOCKED_COLOR : Color.white;
        }

        protected virtual void UpdateVisual()
        {
            MergeGradeData data = GradeData;
            if (data == null || mainRenderer == null)
                return;
            mainRenderer.sprite = data.Sprite;

            UpdateShadowVisual(data);
        }

        protected virtual void UpdateShadowVisual(MergeGradeData data)
        {
            if (shadowRenderer == null)
                return;

            if (!data.CustomShadow)
            {
                shadowRenderer.rectTransform.anchoredPosition3D = shadowBasePosition;
                shadowRenderer.rectTransform.localScale = Vector3.one;
                return;
            }

            if (data.CustomShadowSprite != null)
                shadowRenderer.sprite = data.CustomShadowSprite;

            shadowRenderer.rectTransform.anchoredPosition3D = shadowBasePosition + data.ShadowPositionOffset;
            shadowRenderer.rectTransform.localScale = data.CustomShadowScale;
        }

        // Used by Client Order tasks to highlight the live instance fulfilling a requirement
        // slot — started when bound, stopped if the order completes/cancels or the instance
        // is consumed/destroyed first.
        public void SetTaskBounce(bool active)
        {
            if (active)
            {
                if (taskBounceTween != null)
                    return;
                taskBounceTween = transform.DOPingPongScale(0.95f, 1.1f, 0.45f, Ease.Type.SineInOut, Ease.Type.SineInOut);
            }
            else
            {
                taskBounceTween?.KillActive();
                taskBounceTween = null;
                transform.localScale = Vector3.one;
            }
        }

        public virtual string GetDisplayName() => $"{GradeData?.DisplayName ?? name}";
        public abstract bool CanDrag();
        public abstract ActionButtonConfig GetActionButton();

        // Task bounce owns the scale while active — the select bounce would fight it for
        // localScale and leave the ping-pong mid-cycle.
        public virtual void OnSelected()
        {
            if (taskBounceTween == null)
                BounceAnimation.Bounce(transform);
        }
        public virtual void OnDeselected() { }

        // Override to return a [Serializable] DTO with type-specific state to persist; the
        // caller (MergeController.CollectSaveData) handles JsonUtility.ToJson — subtypes never
        // touch the JSON conversion themselves.
        public virtual object OnBeforeSave() => null;

        // Override to parse customDataJson (produced by this type's own OnBeforeSave) back into
        // runtime state. Only called when customDataJson is non-empty.
        public virtual void OnAfterLoad(string customDataJson) { }
    }
}
