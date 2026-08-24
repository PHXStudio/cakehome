namespace Watermelon
{
    public enum ActionButtonType
    {
        None,
        Delete,
        Sell,
        Spawn
    }

    public class ActionButtonConfig
    {
        public ActionButtonType Type;
        public string Title;
        public string ExtraText;
        public int CurrencyAmount;

        // Re-tapping an already-selected object fires the action directly (safe-to-repeat
        // actions only — destructive ones must stay behind the explicit button).
        public bool ActivateOnTap;

        public ActionButtonConfig(ActionButtonType type, string title = "", string extraText = "", int currencyAmount = 0, bool activateOnTap = false)
        {
            Type = type;
            Title = title;
            ExtraText = extraText;
            CurrencyAmount = currencyAmount;
            ActivateOnTap = activateOnTap;
        }

        public static readonly ActionButtonConfig None = new ActionButtonConfig(ActionButtonType.None);
    }
}
