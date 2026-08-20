using TMPro;
using UnityEngine;

namespace Watermelon
{
    /// <summary>称号单行：名称 + 已解锁/未解锁。</summary>
    public class AvatarTitleRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text statusText;

        private int index;

        public void Setup(int titleIndex)
        {
            index = titleIndex;

            if (nameText != null)
                nameText.text = AvatarController.Titles[index];

            Refresh();
        }

        public void Refresh()
        {
            if (nameText == null)
                return;

            bool unlocked = AvatarController.IsTitleUnlocked(index);
            if (statusText != null)
                statusText.text = unlocked ? "已获得" : "未解锁";
        }
    }
}
