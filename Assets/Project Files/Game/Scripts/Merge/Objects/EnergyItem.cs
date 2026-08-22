namespace Watermelon
{
    public class EnergyItem : MergeFieldObject
    {
        public override bool CanDrag() => !IsHalfLocked;

        public override string GetDisplayName() => GradeData?.DisplayName;

        // No button (see UISelectionPanel — type None hides it) — re-tapping the selected
        // item picks it up directly, same as SpawnerObject's Spawn action.
        public override ActionButtonConfig GetActionButton() =>
            new ActionButtonConfig(ActionButtonType.None, activateOnTap: true);
    }
}
