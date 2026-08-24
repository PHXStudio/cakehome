using System;

namespace Watermelon
{
    /// <summary>
    /// Reverse bridge from Game.Scripts (template code) back into HotUpdate (cake-specific systems).
    /// Game.Scripts cannot reference HotUpdate types, so HotUpdate assigns these callbacks at startup
    /// and template code invokes them. All callbacks are null-tolerant — the merge gameplay must
    /// keep working even if a bridge was never assigned.
    /// </summary>
    public static class CakeUIBridge
    {
        /// <summary>Opens the IAP store page (assigned by HotUpdate GameController).</summary>
        public static Action OpenStore;

        /// <summary>Raised when a client order is delivered. Parameter: number of orders completed in this call (usually 1).</summary>
        public static Action<int> OrderCompleted;
    }
}
