using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class TutorialController : MonoBehaviour, ISceneSavingReceiver
    {
        private static TutorialController instance;

        [ReadOnly]
        [SerializeField] BaseTutorial[] tutorials;

        [SerializeField] TutorialCanvasController tutorialCanvasController;

        public void Init()
        {
            instance = this;

            tutorialCanvasController.Init();

            foreach (BaseTutorial tutorial in tutorials)
            {
                if(!tutorial.IsInitialised)
                    tutorial.Init();
            }
        }

        private void OnDestroy()
        {
            foreach (BaseTutorial tutorial in tutorials)
            {
                if(tutorial.IsInitialised)
                    tutorial.Unload();
            }

            instance = null;
        }

        public static T GetTutorial<T>() where T : BaseTutorial
        {
            TutorialController tutorialController = instance;

            if (tutorialController == null)
            {
                Debug.LogWarning("[TutorialController] GetTutorial called before Init.");
                return null;
            }

            BaseTutorial[] tutorials = tutorialController.tutorials;
            for (int i = 0; i < tutorials.Length; i++)
            {
                if (tutorials[i] is T tutorial)
                    return tutorial;
            }

            return null;
        }

        public static void ActivateTutorial<T>() where T : BaseTutorial
        {
            if (instance == null)
            {
                Debug.LogWarning("[TutorialController] ActivateTutorial called before Init.");
                return;
            }

            T tutorial = GetTutorial<T>();

            if (tutorial == null) return;

            if (!tutorial.IsInitialised)
            {
                tutorial.Init();
            }

            tutorial.StartTutorial();
        }

        public void OnSceneSaving()
        {
            List<BaseTutorial> found = new List<BaseTutorial>();

            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                found.AddRange(root.GetComponentsInChildren<BaseTutorial>(true));

            tutorials = found.ToArray();
        }
    }
}
