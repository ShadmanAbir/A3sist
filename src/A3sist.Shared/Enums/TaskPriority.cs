namespace A3sist.Shared.Enums
{
    /// <summary>
    /// Priority levels for task queue items
    /// </summary>
    public enum TaskPriority
    {
        /// <summary>
        /// Low priority task
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority task (default)
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority task
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority task
        /// </summary>
        Critical = 3
    }
}