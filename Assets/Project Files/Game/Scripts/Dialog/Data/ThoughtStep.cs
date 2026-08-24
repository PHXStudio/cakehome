using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ThoughtStep : DialogStep
    {
        [SerializeField] public CharacterData character;
        [SerializeField] public EmotionType   emotion;
        [SerializeField][TextArea] public string text;

        public override bool IsSkippable => true;
    }
}
