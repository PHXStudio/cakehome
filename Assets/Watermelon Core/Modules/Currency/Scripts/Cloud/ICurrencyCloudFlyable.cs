using UnityEngine;

namespace Watermelon
{
    // Optional hook for a custom CurrencyCloud prefab (e.g. one with shine/trail child
    // effects around the icon). Implement on the prefab's root component and CurrencyCloud
    // will drive icon assignment and the fly fade through it instead of requiring a plain
    // root Image — see CurrencyCloudSettings(name, prefab, sprite).
    public interface ICurrencyCloudFlyable
    {
        void SetIcon(Sprite sprite);
        void SetAlpha(float alpha);

        // Called once per flight, right as the element appears at the source point (before the
        // fade-in starts) — start a shine/appear effect here.
        void OnFlightStart();

        // Called once per flight, right as the element lands on the target (before it's
        // deactivated back into the pool) — stop a trail / play a landing effect here.
        void OnFlightEnd();
    }
}
