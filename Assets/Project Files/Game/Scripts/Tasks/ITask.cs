using System;

namespace Watermelon
{
    public interface ITask
    {
        string TaskId { get; }
        bool   IsComplete { get; }

        // Fired whenever this task's persisted state changes — TaskController uses it to
        // flush the zone-scoped save without every task implementation calling it directly.
        event Action OnProgressChanged;
    }
}
