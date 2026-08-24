#if UNITY_EDITOR
using UnityEngine;

namespace Watermelon
{
    public class DialogDebugSpawner : MonoBehaviour
    {
        [SerializeField] DialogData dialogData;

        public void PlayDialog()
        {
            DialogController.Play(dialogData, () => Debug.Log("[DialogDebugSpawner] Dialog complete"));
        }
    }
}
#endif
