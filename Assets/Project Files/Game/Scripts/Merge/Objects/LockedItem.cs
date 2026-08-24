namespace Watermelon
{
    // Locking state is handled by MergeFieldObject.IsHalfLocked on any object type.
    // This class is not instantiated directly; retained as a stub.
    public class LockedItem : MergeFieldObject
    {
        public override bool CanDrag() => false;
        public override ActionButtonConfig GetActionButton() => ActionButtonConfig.None;
    }
}
