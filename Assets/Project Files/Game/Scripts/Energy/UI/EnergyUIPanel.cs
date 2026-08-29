using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class EnergyUIPanel : MonoBehaviour
    {
        public const string CLOUD_KEY = "Energy";

        [SerializeField] TextMeshProUGUI text;
        [SerializeField] GameObject timerObject;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] Image icon;
        [SerializeField] Button addButton;

        [Space]
        [SerializeField] AudioClip appearAudioClip;
        [SerializeField] AudioClip collectAudioClip;

        public Image Image => icon;
        public Button AddButton => addButton;

        private RectTransform rectTransformRef;
        public RectTransform RectTransform => rectTransformRef;

        public RectTransform TextRectTransform => text.rectTransform;

        private bool isActive;
        private bool cloudCaseRegistered;

        private Coroutine timerCoroutine;
        private readonly StringBuilder sb = new StringBuilder();

        // Tutorial-only gate — while locked, the "+" button is a no-op, so the onboarding flow
        // can't be derailed by the player opening the energy recovery popup.
        private static bool isAddButtonLocked;

        public static void SetAddButtonLocked(bool locked)
        {
            isAddButtonLocked = locked;
        }

        private void Awake()
        {
            rectTransformRef = GetComponent<RectTransform>();
            addButton.onClick.AddListener(OnAddButtonClicked);
            Redraw();
        }

        // Self-initializes so instances dropped into pages other than UIHeader
        // (UIMainMenu / UIGameOver / UIStore) work without an explicit Init() call.
        // RegisterCloudCase / Activate are both guarded against double execution.
        private void Start()
        {
            Init();
        }

        private void OnAddButtonClicked()
        {
            if (isAddButtonLocked) return;

            UIRecoverEnergy.Show();
        }

        public void Init()
        {
            RegisterCloudCase();

            Redraw();
            Activate();
        }

        private void RegisterCloudCase()
        {
            if (cloudCaseRegistered) return;
            cloudCaseRegistered = true;

            // Multiple panel instances exist across pages (UIMainMenu/UIGameOver/UIStore/UIHeader) —
            // the cloud case is global, register it only once.
            if (CurrencyCloud.IsRegistered(CLOUD_KEY)) return;

            if (icon != null && icon.sprite != null)
                CurrencyCloud.RegisterCase(new CurrencyCloudSettings(CLOUD_KEY, icon.sprite, new Vector2(80, 80))
                    .SetAudio(appearAudioClip, collectAudioClip));
        }

        public void Redraw()
        {
            text.text = $"{EnergyController.Current}";
            addButton.gameObject.SetActive(EnergyController.Current < EnergyController.Max);
            RedrawTimer();
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;

            EnergyController.OnEnergyChanged += OnEnergyChanged;

            // 页面用 canvas.enabled 显隐（OnEnable 不触发），隐藏期间退订会错过能量恢复/消耗，
            // 重新订阅时补一次 Redraw，否则数值停留在隐藏前的旧值（同金币面板的修复）。
            Redraw();

            timerCoroutine = StartCoroutine(TimerLoop());
        }

        public void Disable()
        {
            if (!isActive) return;
            isActive = false;

            EnergyController.OnEnergyChanged -= OnEnergyChanged;

            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private void OnEnergyChanged(int current, int max)
        {
            text.text = $"{current}";
            addButton.gameObject.SetActive(current < max);
            RedrawTimer();
        }

        private IEnumerator TimerLoop()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                RedrawTimer();
                yield return wait;
            }
        }

        private void RedrawTimer()
        {
            if (timerObject == null || timerText == null) return;

            if (EnergyController.Current >= EnergyController.Max)
            {
                timerObject.SetActive(false);
                return;
            }

            timerObject.SetActive(true);

            TimeSpan time = TimeSpan.FromSeconds(EnergyController.GetSecondsUntilNext());
            timerText.text = FormatTimer(time);
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

        // Scene unload can destroy the panel without OnHide being called —
        // unsubscribe here so the static event doesn't keep a dead instance
        private void OnDestroy()
        {
            Disable();
        }
    }
}
