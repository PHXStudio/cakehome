namespace Watermelon
{
    /// <summary>
    /// Interface for components that live inside a <see cref="UIPage"/> and need to react to the page's lifecycle.
    /// Implementors are automatically discovered and cached by the page via <c>GetComponentsInChildren</c>
    /// (serialized in the Editor; see <see cref="UIPage.OnSceneSaving"/>).
    /// </summary>
    public interface IUIPageElement
    {
        /// <summary>Called once when the parent page is prepared. Use to cache references or subscribe to events.</summary>
        public void Init(UIPage page);
        /// <summary>Called whenever the parent page's canvas is enabled (<c>true</c>) or disabled (<c>false</c>).</summary>
        public void OnPageStateChanged(bool state);
    }
}