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
            // 碎片功能已禁用（2026-08-24）：恢复时删除下面这行，并重新激活场景里的
            // UI Merge Game/Safe Area/Bottom Panel/Fragment Button + UIClientOrderCard 的碎片发放
            return;

#pragma warning disable CS0162
            if (Random.value > DROP_CHANCE)
                return;

            FragmentController.Add(1);
#pragma warning restore CS0162
        }
    }
}
