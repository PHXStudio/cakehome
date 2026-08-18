using System;
using UnityEngine;

namespace Watermelon
{
    public class GameplayTimer
    {
        public float MaxTime { get; private set; }
        public float CurrentTime { get; private set; }
        public TimeSpan CurrentTimeSpan { get; private set; }

        public bool IsActive { get; private set; }

        public event SimpleCallback OnTimerFinished;

        public delegate void TimeSpanCallback(TimeSpan timespan);
        public event TimeSpanCallback OnTimeSpanChanged;

        public void Start()
        {
            IsActive = true;

            CurrentTime = MaxTime;
            CurrentTimeSpan = TimeSpan.FromSeconds(CurrentTime);
        }

        public void Update()
        {
            if (!IsActive) return;
            if (UIController.IsPopupOpened) return;

            CurrentTime -= Time.deltaTime;

            if (CurrentTime <= 0)
            {
                IsActive = false;

                CurrentTime = 0;
                CurrentTimeSpan = TimeSpan.Zero;

                OnTimerFinished?.Invoke();
            }
            else
            {
                int prevSeconds = CurrentTimeSpan.Seconds;

                CurrentTimeSpan = TimeSpan.FromSeconds(CurrentTime);
                if (CurrentTimeSpan.Seconds != prevSeconds)
                {
                    OnTimeSpanChanged?.Invoke(CurrentTimeSpan);
                }
            }
        }

        public void Pause()
        {
            IsActive = false;
        }

        public void Resume()
        {
            IsActive = true;
        }

        public void SetMaxTime(float maxTime)
        {
            MaxTime = maxTime;
        }

        public void Reset()
        {
            IsActive = false;
            CurrentTime = MaxTime;
            CurrentTimeSpan = TimeSpan.FromSeconds(CurrentTime);
        }
    }
}
