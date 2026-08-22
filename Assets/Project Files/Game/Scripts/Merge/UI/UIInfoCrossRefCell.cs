using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIInfoCrossRefCell : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] GameObject mysteryImage;
        [SerializeField] GameObject infoButtonRoot;
        [SerializeField] Button infoButton;

        [SerializeField] [Range(0f, 1f)] float fadedAlpha = 0.4f;

        private MergeItemData spawnerItemData;
        private int spawnerGrade;

        private void Awake()
        {
            if (infoButton != null)
                infoButton.onClick.AddListener(OnInfoButtonClicked);
        }

        public void SetIcon(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = Color.white;
            if (mysteryImage != null) mysteryImage.SetActive(false);
        }

        public void SetFaded(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = new Color(1f, 1f, 1f, fadedAlpha);
            if (mysteryImage != null) mysteryImage.SetActive(false);
        }

        public void SetMystery()
        {
            icon.enabled = false;
            if (mysteryImage != null) mysteryImage.SetActive(true);
        }

        // Pass a Spawner's MergeItemData to show the info button; pass null to hide it.
        public void SetSpawnerInfo(MergeItemData itemData, int grade)
        {
            spawnerItemData = itemData;
            spawnerGrade = grade;

            if (infoButtonRoot != null)
                infoButtonRoot.SetActive(itemData != null);
        }

        private void OnInfoButtonClicked()
        {
            if (spawnerItemData != null)
                UIInfoWindow.Show(spawnerItemData, spawnerGrade);
        }
    }
}
