using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class RewardFlyElement : MonoBehaviour, ICurrencyCloudFlyable
    {
        [SerializeField] Image image;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] ParticleSystem shineParticle;
        [SerializeField] ParticleSystem trailParticle;

        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }

        public void SetIcon(Sprite sprite)
        {
            image.sprite = sprite;
        }

        public void OnFlightStart()
        {
            shineParticle.Play();
            trailParticle.Play();
        }

        public void OnFlightEnd()
        {
            shineParticle.Stop();
            trailParticle.Stop();
        }
    }
}