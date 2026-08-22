using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class TutorialCanvasController : MonoBehaviour
    {
        private static TutorialCanvasController instance;

        [SerializeField] CanvasGroup fadeCanvasGroup;

        [Space]
        [SerializeField] Animator pointerAnimator;

        private static Canvas tutorialCanvas;

        private static List<TransformCase> activeTransformCases;

        private static TweenCase fadeTweenCase;
        private static TweenCase dragLoopTweenCase;

        private static readonly int DRAG_ANIMATOR_PARAM = Animator.StringToHash("Drag");

        // Must match the "Drag Start" / "Drag End" clip lengths in PointerHolder.controller:
        // those clips drive the grab/release scale+fade now, this only times the handoff to the next step.
        private const float DRAG_LOOP_GRAB_DURATION = 1f / 3f;
        private const float DRAG_LOOP_RELEASE_DURATION = 1f / 3f;
        private const float DRAG_LOOP_REPEAT_DELAY = 0.5f;

        // On scene unload, GameObject destruction order across separate tutorial objects isn't
        // guaranteed: a tutorial's cleanup (fired from its own OnDestroy) can still call into this
        // controller after its canvas hierarchy (and pointerAnimator) has already been destroyed.
        private static bool IsValid => instance != null && instance.pointerAnimator != null;

        public void Init()
        {
            instance = this;

            tutorialCanvas = GetComponent<Canvas>();
            tutorialCanvas.enabled = false;

            activeTransformCases = new List<TransformCase>();

            foreach (ITutorialCanvasElement element in GetComponentsInChildren<ITutorialCanvasElement>(true))
            {
                element.Init();
            }
        }

        private void OnDestroy()
        {
            foreach (TransformCase transformCase in activeTransformCases)
            {
                transformCase.Destroy();
            }
            activeTransformCases.Clear();

            fadeTweenCase.KillActive();
            dragLoopTweenCase.KillActive();
        }

        public static void ActivatePointer(Vector3 position, int animationHash)
        {
            if (!IsValid) return;

            Transform pointerTransform = instance.pointerAnimator.transform;
            pointerTransform.gameObject.SetActive(true);
            pointerTransform.SetAsLastSibling();

            tutorialCanvas.enabled = true;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RectTransform rectTransform = (RectTransform)pointerTransform;

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(position);

            RectTransform canvasRect = UIController.MainCanvas.GetComponent<RectTransform>();

            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, mainCamera, out localPos))
            {
                rectTransform.localPosition = localPos;
            }

            instance.pointerAnimator.Play(animationHash, -1, 0);
        }

        public static void RepositionPointer(Vector3 worldPosition)
        {
            if (!IsValid) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RectTransform rectTransform = (RectTransform)instance.pointerAnimator.transform;
            RectTransform canvasRect = UIController.MainCanvas.GetComponent<RectTransform>();

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, mainCamera, out localPos))
            {
                rectTransform.localPosition = localPos;
            }
        }

        // Tutorial Overlay is a sibling of every UI page under UI Main Canvas (not a separate
        // root canvas), so its own sibling index controls which pages it renders above. Pages
        // are never reparented/reordered by UIController (just shown/hidden via Canvas.enabled),
        // so "above pageX" stays correct until the next call. Call this whenever a step's
        // highlight/pointer/bubble targets something on a specific page, so the overlay doesn't
        // bleed over other pages (e.g. Main Menu, Store) shown before/after it.
        public static void SetSiblingAbovePage(UIPage page)
        {
            if (!IsValid || page == null) return;

            instance.transform.SetSiblingIndex(page.transform.GetSiblingIndex() + 1);
        }

        public static void ActivateTutorialCanvas(RectTransform element, bool createDummy, bool fadeImage)
        {
            if (!IsValid) return;

            TransformCase activeTransformCase = new TransformCase(element);
            activeTransformCase.SetNewParent(tutorialCanvas.transform, createDummy);

            activeTransformCases.Add(activeTransformCase);

            tutorialCanvas.enabled = true;

            if (fadeImage)
            {
                if (!instance.fadeCanvasGroup.gameObject.activeSelf)
                {
                    fadeTweenCase.KillActive();

                    instance.fadeCanvasGroup.gameObject.SetActive(true);
                    instance.fadeCanvasGroup.alpha = 0;

                    fadeTweenCase = instance.fadeCanvasGroup.DOFade(1.0f, 0.3f);
                }
            }
        }

        public static void ResetTutorialCanvas()
        {
            if (!IsValid) return;

            foreach (TransformCase transformCase in activeTransformCases)
            {
                transformCase.Reset();
            }
            activeTransformCases.Clear();

            fadeTweenCase.KillActive();

            instance.fadeCanvasGroup.alpha = 0;
            instance.fadeCanvasGroup.gameObject.SetActive(false);

            instance.pointerAnimator.gameObject.SetActive(false);

            tutorialCanvas.enabled = false;

            instance.transform.SetAsLastSibling();
        }

        public static void ResetPointer()
        {
            if (!IsValid) return;

            StopDragLoopPointer();

            instance.pointerAnimator.gameObject.SetActive(false);

            tutorialCanvas.enabled = false;

            instance.transform.SetAsLastSibling();
        }

        // Loops the hand pointer through a full drag gesture between two world positions:
        // Animator plays grab/release (Drag Start/Idle/End via the "Drag" bool), code drives the move + repeat.
        public static void ActivateDragLoopPointer(Vector3 worldPosA, Vector3 worldPosB, float segmentDuration = 0.8f)
        {
            if (!IsValid) return;

            dragLoopTweenCase.KillActive();

            tutorialCanvas.enabled = true;

            instance.pointerAnimator.gameObject.SetActive(true);
            instance.pointerAnimator.transform.SetAsLastSibling();

            instance.pointerAnimator.SetBool(DRAG_ANIMATOR_PARAM, false);
            instance.pointerAnimator.Play("Idle", 0, 0f);

            PlayDragLoopGrab(worldPosA, worldPosB, segmentDuration);
        }

        private static void PlayDragLoopGrab(Vector3 from, Vector3 to, float segmentDuration)
        {
            RepositionPointer(from);

            instance.pointerAnimator.SetBool(DRAG_ANIMATOR_PARAM, true);
            instance.pointerAnimator.Play("Drag Start", 0, 0f);

            dragLoopTweenCase = Tween.DelayedCall(DRAG_LOOP_GRAB_DURATION, () => PlayDragLoopMove(from, to, segmentDuration));
        }

        private static void PlayDragLoopMove(Vector3 from, Vector3 to, float segmentDuration)
        {
            dragLoopTweenCase = Tween.DoFloat(0f, 1f, segmentDuration, t => RepositionPointer(Vector3.Lerp(from, to, t)))
                .SetEasing(Ease.Type.SineInOut)
                .OnComplete(() => PlayDragLoopRelease(from, to, segmentDuration));
        }

        private static void PlayDragLoopRelease(Vector3 from, Vector3 to, float segmentDuration)
        {
            instance.pointerAnimator.SetBool(DRAG_ANIMATOR_PARAM, false);

            dragLoopTweenCase = Tween.DelayedCall(DRAG_LOOP_RELEASE_DURATION, () =>
            {
                dragLoopTweenCase = Tween.DelayedCall(DRAG_LOOP_REPEAT_DELAY, () => PlayDragLoopGrab(from, to, segmentDuration));
            });
        }

        public static void StopDragLoopPointer()
        {
            dragLoopTweenCase.KillActive();

            if (!IsValid) return;

            instance.pointerAnimator.SetBool(DRAG_ANIMATOR_PARAM, false);

            if (instance.pointerAnimator.gameObject.activeInHierarchy)
                instance.pointerAnimator.Play("Idle", 0, 0f);
        }

        public static void AlignToCorner(RectTransform rectTransform, UIAnchorCorner corner, Vector2 anchoredPosition)
        {
            Vector2 anchor = Vector2.zero;
            Vector2 pivot = Vector2.zero;

            switch (corner)
            {
                case UIAnchorCorner.TopLeft:
                    anchor = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    break;
                case UIAnchorCorner.TopCenter:
                    anchor = new Vector2(0.5f, 1);
                    pivot = new Vector2(0.5f, 1);
                    break;
                case UIAnchorCorner.TopRight:
                    anchor = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    break;
                case UIAnchorCorner.MiddleLeft:
                    anchor = new Vector2(0, 0.5f);
                    pivot = new Vector2(0, 0.5f);
                    break;
                case UIAnchorCorner.MiddleCenter:
                    anchor = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case UIAnchorCorner.MiddleRight:
                    anchor = new Vector2(1, 0.5f);
                    pivot = new Vector2(1, 0.5f);
                    break;
                case UIAnchorCorner.BottomLeft:
                    anchor = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    break;
                case UIAnchorCorner.BottomCenter:
                    anchor = new Vector2(0.5f, 0);
                    pivot = new Vector2(0.5f, 0);
                    break;
                case UIAnchorCorner.BottomRight:
                    anchor = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    break;
            }

            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private class TransformCase
        {
            private RectTransform rectTransform;

            private Transform parentTransform;

            private Vector3 worldPosition;
            private Vector2 anchoredPosition;
            private Vector2 anchorMin;
            private Vector2 anchorMax;
            private Vector2 pivot;
            private Vector2 size;
            private Vector3 scale;
            private Quaternion rotation;

            private int siblingIndex;

            private GameObject dummyObject;

            private bool isActive;

            private TweenCase tweenCase;

            public TransformCase(RectTransform element)
            {
                rectTransform = element;

                siblingIndex = element.GetSiblingIndex();

                parentTransform = element.parent;

                worldPosition = element.position;
                anchoredPosition = element.anchoredPosition;
                anchorMin = element.anchorMin;
                anchorMax = element.anchorMax;
                pivot = element.pivot;
                size = element.sizeDelta;
                scale = element.localScale;
                rotation = element.localRotation;

                isActive = true;
            }

            public void SetNewParent(Transform transform, bool createDummy)
            {
                Vector3 position = rectTransform.position;
                Transform parentTransform = rectTransform.parent;

                rectTransform.SetParent(transform, true);

                if (createDummy)
                {
                    dummyObject = new GameObject("[TUTORIAL DUMMY]", typeof(RectTransform));

                    RectTransform dummyRectTransform = (RectTransform)dummyObject.transform;
                    dummyRectTransform.SetParent(parentTransform, true);
                    dummyRectTransform.SetSiblingIndex(siblingIndex);

                    Vector3 localPos = dummyRectTransform.localPosition;
                    localPos.z = 0;

                    dummyRectTransform.localPosition = localPos;

                    // A fresh RectTransform defaults to center anchors/pivot, but the stored
                    // anchoredPosition is only meaningful relative to the ORIGINAL element's
                    // anchors/pivot — copy those first or the dummy (and the element that later
                    // snaps to the dummy's position) lands somewhere else entirely.
                    dummyRectTransform.anchorMin = anchorMin;
                    dummyRectTransform.anchorMax = anchorMax;
                    dummyRectTransform.pivot = pivot;

                    dummyRectTransform.anchoredPosition = anchoredPosition;
                    dummyRectTransform.sizeDelta = size;
                    dummyRectTransform.localScale = scale;
                    dummyRectTransform.localRotation = rotation;

                    dummyObject.SetActive(true);

                    if(parentTransform != null)
                    {
                        HorizontalOrVerticalLayoutGroup layoutGroup = parentTransform.GetComponent<HorizontalOrVerticalLayoutGroup>();
                        if (layoutGroup != null)
                        {
                            tweenCase = Tween.NextFrame(() =>
                            {
                                ApplyNewPosition();
                            });
                        }
                        else
                        {
                            ApplyNewPosition();
                        }
                    }
                    else
                    {
                        ApplyNewPosition();
                    }

                    // World-position copying is only valid because the tutorial canvas is nested
                    // under the same root canvas as the elements it highlights, so both sides share
                    // one render mode and scale. Keep it that way — with the tutorial canvas as a
                    // standalone root (e.g. Screen Space - Overlay), positions and scales stop being
                    // comparable and the element visibly jumps and shrinks/grows on reparent.
                    void ApplyNewPosition()
                    {
                        rectTransform.position = dummyRectTransform.position;
                    }
                }

            }

            public void Reset()
            {
                if (!isActive) return;

                isActive = false;

                if (dummyObject != null)
                    GameObject.Destroy(dummyObject);

                rectTransform.SetParent(parentTransform, true);
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = size;
                rectTransform.localScale = scale;
                rectTransform.localRotation = rotation;

                rectTransform.SetSiblingIndex(siblingIndex);
            }

            public void Destroy()
            {
                tweenCase.KillActive();

                isActive = false;
            }
        }

        public enum UIAnchorCorner
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }
    }
}