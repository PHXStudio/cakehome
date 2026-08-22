using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ClientOrderTaskDefinition
    {
        [SerializeField] CharacterData      character;
        [SerializeField] List<OrderItem>    items = new List<OrderItem>();
        [SerializeField] int                coinsReward;

        // overrideCharacter is non-null only when a random pool rolled one — sequential entries
        // always pass null and rely on whatever character this definition was authored with.
        public ITask CreateTask(string taskId, CharacterData overrideCharacter)
        {
            CharacterData chosenCharacter = overrideCharacter != null ? overrideCharacter : character;

            // Deep-copy — OrderItem is mutable, a shallow list copy would leave every spawned
            // task sharing (and silently rewriting) the same requirement objects as this asset.
            List<OrderItem> itemsCopy = items.ConvertAll(i => new OrderItem { typeId = i.typeId, grade = i.grade });

            return new ClientOrderTask(taskId, chosenCharacter, itemsCopy, coinsReward);
        }
    }
}
