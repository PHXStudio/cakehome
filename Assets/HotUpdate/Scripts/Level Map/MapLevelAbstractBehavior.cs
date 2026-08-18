using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon.Map
{
    [StaticUnload]
    public abstract class MapLevelAbstractBehavior : MonoBehaviour
    {
        [SerializeField] protected Button button;
        [SerializeField] protected TMP_Text levelNumber;

        public int LevelId { get; protected set; }

        public static event SimpleIntCallback OnLevelClicked;

        protected virtual void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        public virtual void Init(int id)
        {
            LevelId = id;
            levelNumber.text = $"{id + 1}";

            if (id < MapBehavior.MaxLevelReached)
            {
                InitOpen();
            }
            else if (id == MapBehavior.MaxLevelReached)
            {
                InitCurrent();
            }
            else
            {
                InitClose();
            }
        }

        protected virtual void OnButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            OnLevelClicked?.Invoke(LevelId);
        }

        protected abstract void InitOpen();
        protected abstract void InitClose();
        protected abstract void InitCurrent();

        private static void UnloadStatic()
        {
            OnLevelClicked = null;
        }
    }
}