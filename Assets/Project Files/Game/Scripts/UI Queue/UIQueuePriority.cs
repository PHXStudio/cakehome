namespace Watermelon
{
    // Central place for UIQueueController priorities. Lower value runs first among entries
    // waiting at the same time. Leave gaps between values so new windows can slot in later
    // without renumbering everything else.
    public static class UIQueuePriority
    {
        public const int BuildingProgression = -100;
        public const int Dialog              = 0;
        public const int ExpFly              = 5;
        public const int LevelUp             = 10;
    }
}
