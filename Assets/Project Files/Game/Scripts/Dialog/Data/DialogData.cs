using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Dialog", menuName = "Game/Dialog")]
    public class DialogData : ScriptableObject
    {
        [SerializeField] string chapterTitle;
        [SerializeReference] DialogStep[] steps;

        public string        ChapterTitle => chapterTitle;
        public DialogStep[]  Steps        => steps;

        public static DialogData CreateRuntime(string chapterTitle, DialogStep[] steps)
        {
            DialogData data = CreateInstance<DialogData>();
            data.chapterTitle = chapterTitle;
            data.steps = steps;
            return data;
        }
    }
}
