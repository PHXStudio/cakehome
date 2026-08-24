namespace Watermelon
{
    // Sequential = wait for the previous entry/batch to complete before adding this one.
    // Parallel   = add immediately alongside the previous entry, no wait.
    public enum ProgressionTaskAddMode
    {
        Sequential,
        Parallel
    }
}
