using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Network Check", false)]
    public class NetworkCheckInitModule : InitModule
    {
        public override string ModuleName => "Network Check";

        [SerializeField] GameObject popupPrefab;

        public override IEnumerator InitAsync(GameObject owner)
        {
            if(popupPrefab != null)
            {
                GameObject popupObject = Instantiate(popupPrefab);
                popupObject.name = "[Network Check Popup]";

                NetworkCheckPopup networkCheckPopup = popupObject.GetComponent<NetworkCheckPopup>();
                networkCheckPopup.Init();
            }

            yield break;
        }
    }
}
