using System;
using System.Collections.Generic;

namespace Watermelon
{
    [Serializable]
    public class MergeSave : ISaveObject
    {
        public List<CellSaveData> Cells = new List<CellSaveData>();

        // Cells is kept current by MergeController.SyncSaveData() after every board-changing
        // action — no pull-on-save needed. A pull here would run at an unpredictable point during
        // scene/editor teardown, where grid occupants may already be destroyed: Unity's overloaded
        // == on UnityEngine.Object then makes live occupant references look null, so a rebuild at
        // that point would silently wipe Cells down to an empty board instead of leaving it alone.
        public void OnBeforeSave() { }
    }
}
