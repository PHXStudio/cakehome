namespace Watermelon
{
    public class MergeItem : MergeFieldObject
    {
        public override bool CanDrag() => !IsHalfLocked;

        public override ActionButtonConfig GetActionButton()
        {
            int sellPrice = GradeData?.GetConfig<ItemGradeConfig>()?.SellPrice ?? 0;
            if (sellPrice > 0)
                return new ActionButtonConfig(ActionButtonType.Sell, "Sell", $"+{sellPrice}", sellPrice);
            return new ActionButtonConfig(ActionButtonType.Delete, "Delete");
        }
    }
}
