namespace Watermelon
{
    /// <summary>
    /// Contract for a fullscreen overlay panel managed by <see cref="Overlay"/>.
    /// Implement this on a MonoBehaviour child of the <see cref="OverlayPreInitializer"/> GameObject
    /// to provide a custom overlay appearance and animation.
    /// </summary>
    public interface IOverlayPanel
    {
        /// <summary>Returns <c>true</c> when the overlay canvas is currently enabled.</summary>
        public bool IsActive { get; }

        /// <summary>Called once after the panel is found or created. Use to build dynamic visuals.</summary>
        public void Init();
        /// <summary>Called on unload. Clean up resources and destroy the panel GameObject if needed.</summary>
        public void Clear();

        /// <summary>Starts the show animation and invokes <paramref name="onCompleted"/> when finished.</summary>
        public void Show(float duration, SimpleCallback onCompleted);
        /// <summary>Starts the hide animation and invokes <paramref name="onCompleted"/> when finished.</summary>
        public void Hide(float duration, SimpleCallback onCompleted);

        /// <summary>Enables or disables the overlay canvas immediately without animation.</summary>
        public void SetState(bool state);
        /// <summary>Shows or hides the optional loading indicator child object.</summary>
        public void SetLoadingState(bool state);
    }
}
