using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Implements <see cref="IPreInitializable"/> to create the <see cref="Overlay"/> instance
    /// during the Initializer's pre-init phase. Add this component to the Initializer prefab
    /// alongside an optional custom <see cref="IOverlayPanel"/> child.
    /// </summary>
    public class OverlayPreInitializer : MonoBehaviour, IPreInitializable
    {
        private Overlay overlay;

        public void PreInit()
        {
            overlay = new Overlay(gameObject);
        }

        private void OnDestroy()
        {
            overlay?.Unload();
        }
    }
}
