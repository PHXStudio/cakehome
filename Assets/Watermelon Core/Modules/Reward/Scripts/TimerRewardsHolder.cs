using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public sealed class TimerRewardsHolder : RewardsHolder
    {
        private const string DEFAULT_BUTTON_TEXT = "FREE";
        private const float MINIMUM_BUTTON_WIDTH = 270f;

        [Group("Settings")]
        [SerializeField] string saveID = "";

        [Group("Settings"), Space]
        [SerializeField] Button button;

        [Group("Settings")]
        [SerializeField] TMP_Text timerText;
        [Group("Settings")]
        [SerializeField] int timerDurationInMinutes;

        private SimpleLongSave save;
        private DateTime timerStartTime;

        private StringBuilder sb;

        private void Start()
        {
            if (string.IsNullOrEmpty(saveID))
            {
                Debug.LogError($"[TimerRewardsHolder] saveID is empty on '{gameObject.name}'. Assign a unique ID in the Inspector.", gameObject);
                return;
            }

            InitializeComponents();

            save = SaveController.GetSaveObject<SimpleLongSave>($"TimerProduct_{saveID}");

            timerStartTime = DateTime.FromBinary(save.Value);

            sb = new StringBuilder();

            button.onClick.AddListener(OnButtonClicked);
        }

        private string FormatTimer(TimeSpan timeSpan)
        {
            sb.Clear();

            if (timeSpan.Hours > 0)
            {
                sb.Append(timeSpan.Hours);
                sb.Append(':');
            }

            sb.Append(timeSpan.Minutes.ToString("00"));
            sb.Append(':');
            sb.Append(timeSpan.Seconds.ToString("00"));

            return sb.ToString();
        }

        private void Update()
        {
            TimeSpan timer = DateTime.Now - timerStartTime;
            TimeSpan duration = TimeSpan.FromMinutes(timerDurationInMinutes);

            if (timer > duration)
            {
                button.enabled = true;
                timerText.text = DEFAULT_BUTTON_TEXT;
            }
            else
            {
                button.enabled = false;
                timerText.text = FormatTimer(duration - timer);

                float preferredWidth = timerText.preferredWidth;
                if (preferredWidth < MINIMUM_BUTTON_WIDTH) preferredWidth = MINIMUM_BUTTON_WIDTH;

                timerText.rectTransform.sizeDelta = timerText.rectTransform.sizeDelta.SetX(preferredWidth + 5);
                button.image.rectTransform.sizeDelta = button.image.rectTransform.sizeDelta.SetX(preferredWidth + 10);
            }
        }

        public bool IsAvailable()
        {
            TimeSpan timer = DateTime.Now - timerStartTime;
            TimeSpan duration = TimeSpan.FromMinutes(timerDurationInMinutes);

            return timer > duration;
        }

        private void OnButtonClicked()
        {
            save.Value = DateTime.Now.ToBinary();
            timerStartTime = DateTime.Now;

            rewardSet.ApplyReward();

            SaveController.MarkAsSaveIsRequired();
        }
    }
}
