namespace A3sist.Shared.Models
{
    /// <summary>
    /// System health and resource usage metrics
    /// </summary>
    public class SystemHealthMetrics
    {
        /// <summary>
        /// When the metrics were collected
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// CPU usage metrics
        /// </summary>
        public CpuMetrics Cpu { get; set; } = new();

        /// <summary>
        /// Memory usage metrics
        /// </summary>
        public MemoryMetrics Memory { get; set; } = new();

        /// <summary>
        /// Disk usage metrics
        /// </summary>
        public DiskMetrics Disk { get; set; } = new();

        /// <summary>
        /// Application-specific metrics
        /// </summary>
        public ApplicationMetrics Application { get; set; } = new();

        /// <summary>
        /// Agent performance metrics
        /// </summary>
        public Dictionary<string, AgentHealthMetrics> Agents { get; set; } = new();
    }

    /// <summary>
    /// CPU usage metrics
    /// </summary>
    public class CpuMetrics
    {
        /// <summary>
        /// CPU usage percentage (0-100)
        /// </summary>
        public double UsagePercent { get; set; }

        /// <summary>
        /// Number of CPU cores
        /// </summary>
        public int CoreCount { get; set; }

        /// <summary>
        /// CPU frequency in MHz
        /// </summary>
        public double FrequencyMHz { get; set; }
    }

    /// <summary>
    /// Memory usage metrics
    /// </summary>
    public class MemoryMetrics
    {
        /// <summary>
        /// Total physical memory in bytes
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Available physical memory in bytes
        /// </summary>
        public long AvailableBytes { get; set; }

        /// <summary>
        /// Used physical memory in bytes
        /// </summary>
        public long UsedBytes { get; set; }

        /// <summary>
        /// Memory usage percentage (0-100)
        /// </summary>
        public double UsagePercent { get; set; }

        /// <summary>
        /// Working set memory for current process in bytes
        /// </summary>
        public long WorkingSetBytes { get; set; }

        /// <summary>
        /// Private memory for current process in bytes
        /// </summary>
        public long PrivateMemoryBytes { get; set; }
    }

    /// <summary>
    /// Disk usage metrics
    /// </summary>
    public class DiskMetrics
    {
        /// <summary>
        /// Total disk space in bytes
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Available disk space in bytes
        /// </summary>
        public long AvailableBytes { get; set; }

        /// <summary>
        /// Used disk space in bytes
        /// </summary>
        public long UsedBytes { get; set; }

        /// <summary>
        /// Disk usage percentage (0-100)
        /// </summary>
        public double UsagePercent { get; set; }

        /// <summary>
        /// Disk read operations per second
        /// </summary>
        public double ReadOpsPerSecond { get; set; }

        /// <summary>
        /// Disk write operations per second
        /// </summary>
        public double WriteOpsPerSecond { get; set; }
    }

    /// <summary>
    /// Application-specific metrics
    /// </summary>
    public class ApplicationMetrics
    {
        /// <summary>
        /// Application uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Number of active threads
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Number of loaded assemblies
        /// </summary>
        public int AssemblyCount { get; set; }

        /// <summary>
        /// GC generation 0 collections
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// GC generation 1 collections
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// GC generation 2 collections
        /// </summary>
        public int Gen2Collections { get; set; }

        /// <summary>
        /// Total allocated memory in bytes
        /// </summary>
        public long TotalAllocatedBytes { get; set; }
    }

    /// <summary>
    /// Health metrics for a specific agent
    /// </summary>
    public class AgentHealthMetrics
    {
        /// <summary>
        /// Agent name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the agent
        /// </summary>
        public AgentHealthStatus Status { get; set; }

        /// <summary>
        /// Number of requests processed
        /// </summary>
        public int RequestsProcessed { get; set; }

        /// <summary>
        /// Number of successful requests
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// Number of failed requests
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Current queue size
        /// </summary>
        public int QueueSize { get; set; }

        /// <summary>
        /// Error rate (0-1)
        /// </summary>
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// Health status of an agent
    /// </summary>
    public enum AgentHealthStatus
    {
        /// <summary>
        /// Agent is healthy and functioning normally
        /// </summary>
        Healthy,

        /// <summary>
        /// Agent is experiencing some issues but still functional
        /// </summary>
        Warning,

        /// <summary>
        /// Agent is not functioning properly
        /// </summary>
        Critical,

        /// <summary>
        /// Agent is not responding
        /// </summary>
        Unavailable
    }
}