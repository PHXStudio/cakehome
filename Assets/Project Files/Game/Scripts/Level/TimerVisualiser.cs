using System;
using TMPro;
using UnityEngine;

namespace Watermelon
{
    public class TimerVisualiser : MonoBehaviour
    {
        [SerializeField] TMP_Text timerText;
        private GameplayTimer timer;

        [SerializeField] SlicedFilledImage fillImage;

        public void Show(GameplayTimer timer)
        {
            this.timer = timer;
            gameObject.SetActive(true);

            timer.OnTimeSpanChanged += OnTimeChanged;
            OnTimeChanged(timer.CurrentTimeSpan);
        }

        private void OnDestroy()
        {
            if (timer != null)
                timer.OnTimeSpanChanged -= OnTimeChanged;
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            timer.OnTimeSpanChanged -= OnTimeChanged;
        }

        public void SetFreezeFillAmount(float t)
        {
            fillImage.fillAmount = t;
        }

        public void OnTimeChanged(TimeSpan timeSpan)
        {
            timerText.text = string.Format("{0:mm\\:ss}", timeSpan);
        }
    }
}
