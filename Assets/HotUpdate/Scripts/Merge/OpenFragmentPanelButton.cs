using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Small entry button living on the merge board page — opens the fragment exchange popup.
    /// Added to the UI Merge Game bottom panel by MergeSceneImporter (menu step 5).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OpenFragmentPanelButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            UIFragmentExchangePanel.Show();

            AudioController.PlaySound(AudioController.GetClip("button_sound"));
        }
    }
}
