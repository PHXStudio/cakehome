using UnityEngine;

namespace Watermelon
{
    // Not a DialogStep subclass on purpose: DialogStepDrawer (useForChildren: true) applies to
    // any DialogStep-typed field and assumes [SerializeReference] semantics, which breaks on a
    // plain embedded field. BuildingController converts this to a ThoughtStep at runtime instead.
    [System.Serializable]
    public class BuildingCompletionThought
    {
        [SerializeField] CharacterData character;
        [SerializeField] EmotionType   emotion;
        [SerializeField][TextArea] string text;

        public CharacterData Character => character;
        public EmotionType   Emotion    => emotion;
        public string        Text       => text;
    }
}
