namespace A3sist.Shared.Enums
{
    /// <summary>
    /// Health status of an agent
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// Health status is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// Agent is healthy and functioning normally
        /// </summary>
        Healthy,

        /// <summary>
        /// Agent is experiencing minor issues but still functional
        /// </summary>
        Warning,

        /// <summary>
        /// Agent is experiencing critical issues and may not function properly
        /// </summary>
        Critical,

        /// <summary>
        /// Agent is not responding or has failed
        /// </summary>
        Unhealthy
    }
}