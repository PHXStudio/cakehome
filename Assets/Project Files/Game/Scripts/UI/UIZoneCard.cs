using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIZoneCard : MonoBehaviour
    {
        [SerializeField] TMP_Text zoneNameText;
        [SerializeField] Image previewImage;
        [SerializeField] GameObject lockObject;
        [SerializeField] TMP_Text lockLevelText;
        [SerializeField] GameObject currentIndicatorObject;
        [SerializeField] Button button;

        public event Action<UIZoneCard> OnClicked;

        public ZoneData Zone { get; private set; }

        public void Init(ZoneData zone)
        {
            Zone = zone;

            zoneNameText.text = zone.ZoneName;
            previewImage.sprite = zone.ZonePreview;
            previewImage.enabled = zone.ZonePreview != null;
            button.onClick.AddListener(() => OnClicked?.Invoke(this));

            Refresh();
        }

        public void Refresh()
        {
            bool unlocked = ZoneController.IsUnlocked(Zone);

            lockObject.SetActive(!unlocked);
            if (!unlocked)
                lockLevelText.text = $"Lvl {Zone.MinLevelToUnlock}";

            currentIndicatorObject.SetActive(ZoneController.CurrentZone == Zone);
            button.interactable = unlocked;
        }
    }
}
