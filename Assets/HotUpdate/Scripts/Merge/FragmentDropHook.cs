using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tile-match fragment drop hook: on a dock match there is a chance to drop a cake fragment
    /// (carried over from the retired recipe system, same 0.15 feel). Fragments are spent in the
    /// merge side's exchange panel (UIFragmentExchangePanel).
    /// </summary>
    public static class FragmentDropHook
    {
        private const float DROP_CHANCE = 0.15f;

        public static void OnMatchCombined(List<ISlotable> match)
        {
            if (Random.value > DROP_CHANCE)
                return;

            FragmentController.Add(1);
        }
    }
}
