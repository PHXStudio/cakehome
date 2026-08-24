using UnityEngine;

namespace Watermelon
{
    public abstract class BaseTutorial : MonoBehaviour
    {
        private const string SAVE_IDENTIFIER = "TUTORIAL:{0}";

        private bool isActive;
        private bool isFinished;
        protected bool isInitialised;

        public bool IsActive => isActive;
        public bool IsFinished => isFinished;
        public bool IsInitialised => isInitialised;

        protected TutorialBaseSave save;

        internal void Init()
        {
            if (isInitialised) return;

            isInitialised = true;

            save = SaveController.GetSaveObject<TutorialBaseSave>(string.Format(SAVE_IDENTIFIER, GetType().Name));
            isFinished = save.isFinished;

            OnInitialised();
        }

        internal void StartTutorial()
        {
            if (!isInitialised) Init();
            if (isActive || isFinished) return;

            isActive = true;
            Checkpoint.Log($"Tutorial started: {GetType().Name}", gameObject);
            OnStarted();
        }

        protected internal void FinishTutorial()
        {
            if (isFinished) return;

            isActive = false;
            isFinished = true;
            save.isFinished = true;
            SaveController.MarkAsSaveIsRequired();

            Checkpoint.Log($"Tutorial finished: {GetType().Name}", gameObject);

            OnFinished();
        }

        protected abstract void OnInitialised();
        protected abstract void OnStarted();
        protected abstract void OnFinished();

        internal void Unload()
        {
            OnUnloaded();
            isInitialised = false;
        }

        protected virtual void OnUnloaded() { }
    }
}
