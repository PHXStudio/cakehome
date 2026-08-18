using UnityEngine;

namespace Watermelon
{
    public class ShopIdleProducer : MonoBehaviour
    {
        [SerializeField] float tickInterval = 1f;

        private float timer;
        private bool active;

        public void SetActive(bool value)
        {
            active = value;
            if (active)
            {
                ShopController.OnShopOpened();
                timer = 0f;
            }
        }

        private void Update()
        {
            if (!active || !ShopController.IsInitialized)
                return;

            timer += Time.unscaledDeltaTime;
            if (timer < tickInterval)
                return;

            ShopController.TickOnline(timer);
            timer = 0f;
        }
    }
}
