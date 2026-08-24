using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIPlaceholderCard : MonoBehaviour, ITaskCardView
    {
        [SerializeField] Image characterPortrait;
        [SerializeField] LayoutElement layoutElement;

        private const float PreferredWidth = 350f;

        public void Setup(PlaceholderTask task)
        {
            if (task.Character != null)
                characterPortrait.sprite = task.Character.GetPortrait(EmotionType.Default);
        }

        public void PlayEnter() => TaskCardAnimation.PlayEnter(transform, layoutElement, PreferredWidth);
        public void PlayExit(Action onComplete) => TaskCardAnimation.PlayExit(transform, layoutElement, onComplete);
    }
}
