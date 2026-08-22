using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Character Database", menuName = "Game/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [SerializeField] CharacterData[] characters;

        public CharacterData[] Characters => characters;

        public CharacterData GetCharacter(string characterId)
        {
            if (characters == null || string.IsNullOrEmpty(characterId))
            {
                Debug.LogError($"[CharacterDatabase] GetCharacter: null returned (characters array null or characterId empty). characterId: {characterId}");

                return null;
            }

            foreach (CharacterData character in characters)
                if (character != null && character.CharacterId == characterId) return character;

            Debug.LogError($"[CharacterDatabase] GetCharacter: null returned, no character found with id: {characterId}");

            return null;
        }
    }
}
