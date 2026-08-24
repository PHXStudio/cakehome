using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Characters")]
    public class CharacterInitModule : InitModule
    {
        public override string ModuleName => "Characters";

        [SerializeField] CharacterDatabase charactersDatabase;

        private CharacterController characterController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            characterController = new CharacterController(charactersDatabase);
            yield break;
        }

        public override void Unload()
        {
            characterController?.Unload();
        }
    }
}
