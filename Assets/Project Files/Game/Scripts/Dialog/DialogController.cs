using System;

namespace Watermelon
{
    public class DialogController
    {
        private static DialogController instance;

        private UIDialog activePanel;

        public DialogController()
        {
            instance = this;
        }

        public static void RegisterPanel(UIDialog panel)
        {
            if (instance != null) instance.activePanel = panel;
        }

        public static void UnregisterPanel(UIDialog panel)
        {
            if (instance != null && instance.activePanel == panel)
                instance.activePanel = null;
        }

        public static void Play(DialogData data, Action onComplete)
        {
            void OnDialogComplete()
            {
                SavePresets.CreateSave(data.name, "Dialogs");

                Checkpoint.Log($"Dialog finished: {data.name}");

                onComplete?.Invoke();
            }

            Checkpoint.Log($"Dialog started: {data.name}");

            if (instance?.activePanel == null)
            {
                OnDialogComplete();
                return;
            }
            instance.activePanel.Play(data, OnDialogComplete);
        }

        public void Unload()
        {
            instance = null;
        }
    }
}
