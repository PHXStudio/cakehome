using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class ExperienceBadge : MonoBehaviour
    {
        private const string CLOUD_KEY = "Exp";
        private const float  FILL_DURATION = 0.3f;

        [SerializeField] TextMeshProUGUI levelText;
        [SerializeField] Image           fillImage;

        [Header("Fly")]
        [Tooltip("Optional custom CurrencyCloud prefab (shine/trail, see RewardFlyElement). Falls back to a plain icon Image if left empty.")]
        [SerializeField] GameObject customFlyPrefab;

        private RectTransform rectTransformRef;
        public RectTransform RectTransform => rectTransformRef;

        private bool isActive;
        private bool cloudCaseRegistered;

        // Level currently reflected by the fill ring/text — lags behind ExperienceController.CurrentLevel
        // while a level-up lap animation is still playing.
        private int displayedLevel;

        private void Awake()
        {
            rectTransformRef = GetComponent<RectTransform>();
        }

        private void OnDestroy()
        {
            Disable();
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

            Sprite icon = ExperienceController.ExpIcon;
            if (icon == null) return;

            CurrencyCloudSettings settings = customFlyPrefab != null
                ? new CurrencyCloudSettings(CLOUD_KEY, customFlyPrefab, icon)
                : new CurrencyCloudSettings(CLOUD_KEY, icon, new Vector2(80, 80));

            settings.SetAudio(ExperienceController.AppearAudioClip, ExperienceController.CollectAudioClip);

            CurrencyCloud.RegisterCase(settings);
        }

        private void Redraw()
        {
            displayedLevel = ExperienceController.CurrentLevel;

            levelText.text = displayedLevel.ToString();
            fillImage.fillAmount = ExperienceController.CurrentLevelProgress;
        }

        public void Activate()
        {
            if (isActive) return;
            isActive = true;

            ExperienceController.OnProgressChanged += OnProgressChanged;
        }

        public void Disable()
        {
            if (!isActive) return;
            isActive = false;

            ExperienceController.OnProgressChanged -= OnProgressChanged;
        }

        private void OnProgressChanged()
        {
            int levelsGained = ExperienceController.CurrentLevel - displayedLevel;

            if (levelsGained <= 0)
            {
                // Queued (not fired inline) so a Dialog enqueued moments later by the same
                // caller (e.g. BuildingController.TriggerProgression) still claims the turn
                // first — see UIQueuePriority for the ordering rule this depends on. Sourced
                // from screen center, same as the level-up branch below and RewardFlyController's
                // spawner fly, so both entries queued at the same priority read as one flight.
                UIQueueController.Enqueue(UIQueuePriority.ExpFly, onDone =>
                {
                    RectTransform flySource = UIController.GetPage<UIHeader>()?.ScreenCenterRectTransform ?? rectTransformRef;
                    CurrencyCloud.SpawnCurrency(CLOUD_KEY, flySource, levelText.rectTransform, 6, "", () => onDone());
                    fillImage.DOFillAmount(ExperienceController.CurrentLevelProgress, FILL_DURATION);
                });
                return;
            }

            // A level was gained — hold a UIQueueController turn (between Dialog and LevelUp
            // priority) so UILevelUpPopup only opens once BOTH the cloud has actually hit the
            // badge (onCurrencyHittedTarget) and the ring has settled on its final value.
            // Same turn/priority also carries SpawnerQueueReward's icon flight (see
            // SpawnerRewardFlyController) — both fly from the screen center together.
            UIQueueController.Enqueue(UIQueuePriority.ExpFly, onDone =>
            {
                bool cloudHit = false;
                bool ringSettled = false;

                void TryFinish()
                {
                    if (cloudHit && ringSettled)
                        onDone();
                }

                RectTransform flySource = UIController.GetPage<UIHeader>()?.ScreenCenterRectTransform ?? rectTransformRef;
                CurrencyCloud.SpawnCurrency(CLOUD_KEY, flySource, levelText.rectTransform, 6, "", () =>
                {
                    cloudHit = true;
                    TryFinish();
                });

                PlayLap(levelsGained, () =>
                {
                    ringSettled = true;
                    TryFinish();
                });
            });
        }

        // Fills the ring to a full lap, snaps back to empty, bumps the level, and either
        // plays another lap (if more levels were gained) or settles on the final progress —
        // onSettled fires once the ring has settled on that final value.
        private void PlayLap(int lapsRemaining, Action onSettled)
        {
            fillImage.DOFillAmount(1f, FILL_DURATION).OnComplete(() =>
            {
                fillImage.fillAmount = 0f;
                displayedLevel++;
                levelText.text = displayedLevel.ToString();

                if (lapsRemaining > 1)
                    PlayLap(lapsRemaining - 1, onSettled);
                else
                    fillImage.DOFillAmount(ExperienceController.CurrentLevelProgress, FILL_DURATION).OnComplete(() => onSettled?.Invoke());
            });
        }
    }
}
