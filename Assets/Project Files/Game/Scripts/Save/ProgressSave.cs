using System;
using System.Collections.Generic;

namespace Watermelon
{
    [Serializable]
    public class ProgressSave : ISaveObject
    {
        public List<string> SeenDialogIds = new List<string>();
        public int TutorialStep;
        public List<string> ShownNewGeneratorIds = new List<string>();
        public bool ChestTutorialShown;

        public void OnBeforeSave() { }
    }
}
