using System;

namespace Watermelon
{
    /// <summary>
    /// Marks a <see cref="UIPage"/> as a popup window.
    /// </summary>
    [Obsolete("IPopupWindow is deprecated. Override UIPage.IsPopup to return true instead.")]
    public interface IPopupWindow
    {
        /// <summary>Returns <c>true</c> when the popup is currently open and visible.</summary>
        bool IsOpened { get; }
    }
}
