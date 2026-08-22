using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIHammerCard : MonoBehaviour, ITaskCardView
    {
        [SerializeField] LayoutElement layoutElement;

        private Action onClicked;
        private TweenCase bounceTween;
        private float defaultPreferredWidth;

        private void Awake()
        {
            defaultPreferredWidth = layoutElement.preferredWidth;
        }

        public void Setup(Action onClick)
        {
            onClicked = onClick;
        }

        public void OnHammerClicked()
        {
            onClicked?.Invoke();
        }

        public void PlayEnter()
        {
            bounceTween.KillActive();

            TaskCardAnimation.PlayEnter(transform, layoutElement, defaultPreferredWidth);
            bounceTween = transform.DOPingPongScale(0.9f, 1.1f, 0.5f, Ease.Type.SineOut, Ease.Type.SineIn, TaskCardAnimation.EnterDuration);
        }

        public void PlayExit(Action onComplete)
        {
            bounceTween.KillActive();

            TaskCardAnimation.PlayExit(transform, layoutElement, onComplete);
        }
    }
}
