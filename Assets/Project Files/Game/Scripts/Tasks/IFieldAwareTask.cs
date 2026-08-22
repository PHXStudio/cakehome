namespace Watermelon
{
    // Tasks that need to re-check live field state whenever it changes (item still sits on the
    // grid until the task is given) — e.g. ClientOrderTask.
    public interface IFieldAwareTask : ITask
    {
        void OnFieldChanged(MergeGrid grid);
    }
}
