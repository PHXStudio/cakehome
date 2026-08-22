using System;

namespace Watermelon
{
    /// <summary>
    /// Marker interface for popups that should pause the game while open.
    /// </summary>
    [Obsolete("IPausePopup is deprecated. Override UIPage.IsPausePopup to return true instead.")]
    public interface IPausePopup
    {

    }
}
