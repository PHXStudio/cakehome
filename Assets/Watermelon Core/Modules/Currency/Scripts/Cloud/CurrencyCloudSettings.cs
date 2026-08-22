using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class CurrencyCloudSettings
    {
        public const float DEFAULT_RADIUS = 200;

        private string name;
        public string Name => name;

        private float cloudRadius;
        public float CloudRadius => cloudRadius;

        private GameObject prefab;
        public GameObject Prefab => prefab;

        private Sprite sprite;
        public Sprite Sprite => sprite;

        private AudioClip appearAudioClip;
        public AudioClip AppearAudioClip => appearAudioClip;

        private AudioClip collectAudioClip;
        public AudioClip CollectAudioClip => collectAudioClip;

        public CurrencyCloudSettings(string name, GameObject prefab)
        {
            this.name = name;
            this.prefab = prefab;
            this.cloudRadius = DEFAULT_RADIUS;
        }

        public CurrencyCloudSettings(string name, Sprite sprite, Vector2 size)
        {
            this.name = name;
            this.cloudRadius = DEFAULT_RADIUS;

            GameObject tempPrefab = new GameObject(name);
            tempPrefab.hideFlags = HideFlags.HideInHierarchy;

            GameObject.DontDestroyOnLoad(tempPrefab);

            Image image = tempPrefab.AddComponent<Image>();
            image.sprite = sprite;

            RectTransform rectTransform = (RectTransform)tempPrefab.transform;
            rectTransform.sizeDelta = size;

            this.prefab = tempPrefab;
        }

        // Custom prefab + a dynamic sprite (e.g. a per-type reward icon). If the prefab's root
        // implements ICurrencyCloudFlyable (e.g. to route the icon into a shine/trail effect),
        // that's used; otherwise the sprite is linked directly onto a plain Image on the prefab.
        public CurrencyCloudSettings(string name, GameObject prefab, Sprite sprite)
        {
            this.name = name;
            this.cloudRadius = DEFAULT_RADIUS;
            this.sprite = sprite;

            GameObject instance = GameObject.Instantiate(prefab);
            instance.name = name;
            instance.hideFlags = HideFlags.HideInHierarchy;

            GameObject.DontDestroyOnLoad(instance);

            ICurrencyCloudFlyable flyable = instance.GetComponent<ICurrencyCloudFlyable>();
            if (flyable != null)
                flyable.SetIcon(sprite);
            else
            {
                Image image = instance.GetComponent<Image>();
                if (image != null) image.sprite = sprite;
            }

            this.prefab = instance;
        }

        public CurrencyCloudSettings SetAudio(AudioClip appearAudioClip, AudioClip collectAudioClip)
        {
            this.appearAudioClip = appearAudioClip;
            this.collectAudioClip = collectAudioClip;

            return this;
        }

        public CurrencyCloudSettings SetRadius(float cloudRadius)
        {
            this.cloudRadius = cloudRadius;

            return this;
        }
    }
}
