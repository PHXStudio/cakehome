using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    public class BuildingBehavior : MonoBehaviour
    {
        // Particle spawns shortly after the upgrade completes, and the sprite swaps
        // (with a bounce + flash) shortly after the particle is on screen.
        private const float BOUNCE_TO_PARTICLE_DELAY = 0.1f;
        private const float PARTICLE_TO_SPRITE_DELAY = 0.3f;
        private const float SPRITE_SWAP_FLASH_FADE_DURATION = 0.35f;

        [SerializeField] string  buildingId;
        [SerializeField] string  buildingName;
        [SerializeField] Sprite  defaultSprite;
        [SerializeField] Image   buildingImage;
        [SerializeField] UpgradeStepData[] upgrades;
        [SerializeField] SimpleBounce upgradeBounce;

        private TweenCase pendingUpgradeTween;
        private TweenCase flashTweenCase;
        private Image     upgradeFlashImage;

        public string            BuildingId    => buildingId;
        public string            BuildingName  => buildingName;
        public Sprite            DefaultSprite => defaultSprite;
        public UpgradeStepData[] Upgrades      => upgrades;
        public int               MaxUpgrades   => upgrades?.Length ?? 0;

        public ZoneBehavior Zone => zone;

        private ZoneBehavior zone;

        private void OnEnable()
        {
            BuildingController.OnUpgradeCompleted += OnUpgradeCompleted;
        }

        private void OnDisable()
        {
            BuildingController.OnUpgradeCompleted -= OnUpgradeCompleted;

            pendingUpgradeTween.KillActive();
            pendingUpgradeTween = null;

            flashTweenCase.KillActive();
        }

        public void Init(ZoneBehavior owner)
        {
            zone = owner;

            upgradeBounce.Init(buildingImage.transform);
            CreateUpgradeFlashOverlay();

            RefreshVisual();
        }

        private void CreateUpgradeFlashOverlay()
        {
            if (!buildingImage.gameObject.TryGetComponent(out Mask mask))
                mask = buildingImage.gameObject.AddComponent<Mask>();

            mask.showMaskGraphic = true;

            GameObject flashObject = new GameObject("Upgrade Flash", typeof(RectTransform), typeof(Image));

            RectTransform flashRect = flashObject.GetComponent<RectTransform>();
            flashRect.SetParent(buildingImage.rectTransform, false);
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;

            upgradeFlashImage = flashObject.GetComponent<Image>();
            upgradeFlashImage.raycastTarget = false;
            upgradeFlashImage.color = GameData.Data.BuildingUpgradeFlashColor.SetAlpha(0f);
        }

        private void RefreshVisual()
        {
            int step = BuildingController.GetBuildingStep(this);
            buildingImage.sprite = step <= 0 ? defaultSprite : upgrades[step - 1].UpgradedSprite;
        }

        private void FlashUpgradeSprite()
        {
            flashTweenCase.KillActive();

            upgradeFlashImage.color = GameData.Data.BuildingUpgradeFlashColor;
            flashTweenCase = upgradeFlashImage.DOFade(0f, SPRITE_SWAP_FLASH_FADE_DURATION, unscaledTime: true);
        }

        private void OnUpgradeCompleted(ZoneData zoneData, string upgradedBuildingId, int totalUpgradesInZone)
        {
            if (zone == null || zoneData != zone.ZoneData) return;

            if (upgradedBuildingId != buildingId)
            {
                RefreshVisual();
                return;
            }

            pendingUpgradeTween.KillActive();
            pendingUpgradeTween = Tween.DelayedCall(BOUNCE_TO_PARTICLE_DELAY, () =>
            {
                PlayUpgradeParticle();
                AudioController.PlaySound(AudioController.GetClip("building_hammer"));

                pendingUpgradeTween = Tween.DelayedCall(PARTICLE_TO_SPRITE_DELAY, () =>
                {
                    pendingUpgradeTween = null;

                    RefreshVisual();
                    upgradeBounce.Bounce();
                    FlashUpgradeSprite();
                });
            });
        }

        private void PlayUpgradeParticle()
        {
            Sprite      buildingSprite = buildingImage.sprite;
            RectTransform rectTransform = buildingImage.rectTransform;

            Vector2 worldSize  = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
            Vector2 spriteSize = buildingSprite.rect.size / buildingSprite.pixelsPerUnit;

            ParticleCase particleCase = ParticlesController.PlayParticle("Building Upgrade");
            if (particleCase == null) return;

            particleCase.ApplyToParticles(particleSystem =>
            {
                ParticleSystem.ShapeModule shape = particleSystem.shape;
                shape.sprite = buildingSprite;
            });

            particleCase.SetPosition(rectTransform.TransformPoint(rectTransform.rect.center));
            particleCase.SetScale(new Vector3(worldSize.x / spriteSize.x, worldSize.y / spriteSize.y, 1f));
        }

#if UNITY_EDITOR
        // TEMP TEST TOOLS — quick in-editor Play mode check of the upgrade animation sequence.
        // Delete this whole #if UNITY_EDITOR block (incl. previewStep) once verified; not game logic.
        [SerializeField] private int previewStep;

        [Button("Test Upgrade")]
        private void TestUpgrade()
        {
            if (previewStep >= MaxUpgrades) return;
            previewStep++;

            pendingUpgradeTween.KillActive();
            pendingUpgradeTween = Tween.DelayedCall(BOUNCE_TO_PARTICLE_DELAY, () =>
            {
                PlayUpgradeParticle();
                AudioController.PlaySound(AudioController.GetClip("building_hammer"));

                pendingUpgradeTween = Tween.DelayedCall(PARTICLE_TO_SPRITE_DELAY, () =>
                {
                    pendingUpgradeTween = null;

                    ApplyPreviewSprite();
                    upgradeBounce.Bounce();
                    FlashUpgradeSprite();
                });
            });
        }

        [Button("Test Downgrade")]
        private void TestDowngrade()
        {
            pendingUpgradeTween.KillActive();
            pendingUpgradeTween = null;

            previewStep = Mathf.Clamp(previewStep - 1, 0, MaxUpgrades);
            ApplyPreviewSprite();
        }

        private void ApplyPreviewSprite()
        {
            if (buildingImage == null) return;

            buildingImage.sprite = previewStep <= 0 ? defaultSprite : upgrades[previewStep - 1].UpgradedSprite;
        }
#endif
    }
}
