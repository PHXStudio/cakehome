using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Character", menuName = "Game/Character")]
    public class CharacterData : ScriptableObject
    {
        [UniqueID]
        [SerializeField] string characterId;

        [SerializeField] string characterName = "???";
        [SerializeField] Color  portraitColor = Color.white;
        [SerializeField] Sprite defaultPortrait;
        [SerializeField] bool   isMainCharacter;
        [SerializeField] EmotionEntry[] emotions;

        public string CharacterId     => characterId;
        public string CharacterName   => characterName;
        public Color  PortraitColor   => portraitColor;
        public bool   IsMainCharacter => isMainCharacter;

        public Sprite GetPortrait(EmotionType emotion)
        {
            if (emotions != null)
                foreach (var e in emotions)
                    if (e.emotion == emotion) return e.sprite;
            return defaultPortrait;
        }
    }
}
