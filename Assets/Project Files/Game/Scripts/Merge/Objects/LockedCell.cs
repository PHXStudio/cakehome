using System;
using UnityEngine;

namespace Watermelon
{
    public class LockedCell : MergeFieldObject
    {
        [Serializable]
        private struct LockedCellSaveData
        {
            public string HiddenTypeId;
            public int HiddenGrade;
        }

        public string HiddenTypeId { get; set; }
        public int HiddenGrade { get; set; }

        public override string GetDisplayName() => string.Empty;
        public override bool CanDrag() => false;
        public override ActionButtonConfig GetActionButton() => ActionButtonConfig.None;

        public override object OnBeforeSave() =>
            new LockedCellSaveData { HiddenTypeId = HiddenTypeId, HiddenGrade = HiddenGrade };

        public override void OnAfterLoad(string customDataJson)
        {
            LockedCellSaveData data = JsonUtility.FromJson<LockedCellSaveData>(customDataJson);
            HiddenTypeId = data.HiddenTypeId;
            HiddenGrade = data.HiddenGrade;
        }
    }
}
