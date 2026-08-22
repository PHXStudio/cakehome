using UnityEngine;

namespace Watermelon
{
    public abstract class SDKBehavior : MonoBehaviour
    {
        public virtual void Init() { }

        public abstract void OnUserConsentReceived();
    }
}
