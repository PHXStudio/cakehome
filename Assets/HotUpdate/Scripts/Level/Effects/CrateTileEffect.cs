using UnityEngine;

namespace Watermelon
{
    public class CrateTileEffect : TileEffect
    {
        [SerializeField] Sprite crateTileSprite;
        [SerializeField] Particle crateParticle;

        private Sprite storedSprite;
        private SpriteRenderer tileSpriteRenderer;
        private SpriteRenderer iconSpriteRenderer;

        private static int particleHash;

        public override void Init()
        {
            particleHash = ParticlesController.RegisterParticle(crateParticle);
        }

        public override void OnCreated(TileBehavior tileBehavior)
        {
            iconSpriteRenderer = linkedTile.IconSpriteRenderer;
            iconSpriteRenderer.enabled = false;

            tileSpriteRenderer = linkedTile.BackgroundSpriteRenderer;

            storedSprite = tileSpriteRenderer.sprite;
            tileSpriteRenderer.sprite = crateTileSprite;
        }

        public override void OnDisabled(TileBehavior tileBehavior)
        {
            iconSpriteRenderer.enabled = true;
            tileSpriteRenderer.sprite = storedSprite;
        }

        public override void OnAnyTileSubmitted()
        {
            if(linkedTile.IsClickable || !LevelController.IsLevelCompletable())
            {
                if (particleHash != -1)
                    ParticlesController.PlayParticle(particleHash).SetPosition(transform.position);
                
                DisableEffect();

                AudioController.PlaySound(AudioController.AudioClips.crateCrackSound, volumePercentage: 0.5f, minDelay: 0.3f);
            }
        }

        public override void Clear()
        {
            iconSpriteRenderer.enabled = true;
            tileSpriteRenderer.sprite = storedSprite;
        }

        public override bool IsClickAllowed()
        {
            return false;
        }
    }
}
