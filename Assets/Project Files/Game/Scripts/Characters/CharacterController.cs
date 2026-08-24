namespace Watermelon
{
    public class CharacterController
    {
        private static CharacterController instance;

        private readonly CharacterDatabase database;

        public CharacterController(CharacterDatabase database)
        {
            instance = this;
            this.database = database;
        }

        public static CharacterData GetCharacter(string characterId) =>
            instance?.database?.GetCharacter(characterId);

        public void Unload()
        {
            instance = null;
        }
    }
}
