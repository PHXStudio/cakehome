using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class Currency
    {
        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;

        [SerializeField] int defaultAmount = 0;
        public int DefaultAmount => defaultAmount;

        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] CurrencyData data;
        public CurrencyData Data => data;

        [SerializeField] FloatingCloudCase floatingCloud;
        public FloatingCloudCase CurrencyCloud => floatingCloud;

        [SerializeField] CurrencyRewardPreviewSettings previewSettings;
        public CurrencyRewardPreviewSettings PreviewSettings => previewSettings;

        public int Amount { get => save.Amount; set => save.Amount = value; }

        public string AmountFormatted => CurrencyHelper.Format(save.Amount);

        public event CurrencyCallback OnCurrencyChanged;

        private Save save;

        public void Init()
        {
            data.Init(this);

            // Initialize preview settings if available
            previewSettings?.Init(this);
        }

        public void SetSave(Save save)
        {
            this.save = save;
        }

        public void InvokeChangeEvent(int difference)
        {
            OnCurrencyChanged?.Invoke(this, difference);
        }

        // Currency objects live on the CurrencyDatabase asset, which survives scene reloads and
        // (with Enter Play Mode's domain reload disabled) even Play Mode restarts — without this,
        // every scene object that ever subscribed keeps receiving callbacks after it's gone.
        public void ClearListeners()
        {
            OnCurrencyChanged = null;
        }

        [System.Serializable]
        public class Save : ISaveObject
        {
            [SerializeField] int amount = -1;
            public int Amount { get => amount; set => amount = value; }

            public void OnBeforeSave()
            {

            }
        }

        [System.Serializable]
        public class FloatingCloudCase
        {
            [SerializeField] bool addToCloud;
            public bool AddToCloud => addToCloud;

            [SerializeField] float radius = 200;
            public float Radius => radius;

            [SerializeField] GameObject specialPrefab;
            public GameObject SpecialPrefab => specialPrefab;

            [SerializeField] AudioClip appearAudioClip;
            public AudioClip AppearAudioClip => appearAudioClip;

            [SerializeField] AudioClip collectAudioClip;
            public AudioClip CollectAudioClip => collectAudioClip;
        }
    }
}