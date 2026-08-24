using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIInfoGradeCell : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] GameObject selectionImage;
        [SerializeField] GameObject mysteryImage;
        [SerializeField] GameObject arrowImage;
        [SerializeField] TMP_Text levelText;

        [SerializeField] [Range(0f, 1f)] float fadedAlpha = 0.4f;

        public void SetOpen(Sprite sprite, bool isSelected)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = Color.white;
            mysteryImage.SetActive(false);
            selectionImage.SetActive(isSelected);
        }

        public void SetFaded(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = new Color(1f, 1f, 1f, fadedAlpha);
            mysteryImage.SetActive(false);
            selectionImage.SetActive(false);
        }

        public void SetMystery()
        {
            icon.enabled = false;
            mysteryImage.SetActive(true);
            selectionImage.SetActive(false);
        }

        public void SetArrowVisible(bool isVisible)
        {
            arrowImage.SetActive(isVisible);
        }

        public void SetLevel(int grade)
        {
            levelText.text = grade.ToString();
        }
    }
}
