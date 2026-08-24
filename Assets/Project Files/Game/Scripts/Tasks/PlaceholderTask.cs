#pragma warning disable CS0067

using System;

namespace Watermelon
{
    // Filler card shown when RandomProgressionTaskPool's spawn is coin-gated (see
    // TaskController.EvaluateProgressionQueue) so the task panel isn't left empty. Never
    // completes on its own — removed once a building upgrade re-evaluates progression.
    public class PlaceholderTask : ITask
    {
        public string TaskId { get; }
        public CharacterData Character { get; }

        public bool IsComplete => false;

        public event Action OnProgressChanged;

        public PlaceholderTask(string taskId, CharacterData character)
        {
            TaskId    = taskId;
            Character = character;
        }
    }
}
