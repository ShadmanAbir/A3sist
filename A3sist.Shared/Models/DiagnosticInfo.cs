namespace A3sist.Shared.Models
{
    /// <summary>
    /// Comprehensive diagnostic information about the system
    /// </summary>
    public class DiagnosticInfo
    {
        /// <summary>
        /// When the diagnostic information was collected
        /// </summary>
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Application information
        /// </summary>
        public ApplicationDiagnostics Application { get; set; } = new();

        /// <summary>
        /// System information
        /// </summary>
        public SystemDiagnostics System { get; set; } = new();

        /// <summary>
        /// Performance information
        /// </summary>
        public PerformanceDiagnostics Performance { get; set; } = new();

        /// <summary>
        /// Configuration information
        /// </summary>
        public ConfigurationDiagnostics Configuration { get; set; } = new();

        /// <summary>
        /// Agent diagnostics
        /// </summary>
        public Dictionary<string, AgentDiagnostics> Agents { get; set; } = new();

        /// <summary>
        /// Recent errors summary
        /// </summary>
        public ErrorDiagnostics Errors { get; set; } = new();

        /// <summary>
        /// Environment variables (filtered for security)
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        /// <summary>
        /// Loaded assemblies information
        /// </summary>
        public List<AssemblyInfo> LoadedAssemblies { get; set; } = new();

        /// <summary>
        /// Network connectivity information
        /// </summary>
        public NetworkDiagnostics Network { get; set; } = new();
    }

    /// <summary>
    /// Application-specific diagnostic information
    /// </summary>
    public class ApplicationDiagnostics
    {
        /// <summary>
        /// Application name
        /// </summary>
        public string Name { get; set; } = "A3sist";

        /// <summary>
        /// Application version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Build configuration (Debug/Release)
        /// </summary>
        public string BuildConfiguration { get; set; } = string.Empty;

        /// <summary>
        /// Application startup time
        /// </summary>
        public DateTime StartupTime { get; set; }

        /// <summary>
        /// Application uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Current working directory
        /// </summary>
        public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;

        /// <summary>
        /// Application base directory
        /// </summary>
        public string BaseDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Command line arguments
        /// </summary>
        public string[] CommandLineArgs { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Current culture
        /// </summary>
        public string Culture { get; set; } = System.Globalization.CultureInfo.CurrentCulture.Name;

        /// <summary>
        /// UI culture
        /// </summary>
        public string UICulture { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.Name;
    }

    /// <summary>
    /// System diagnostic information
    /// </summary>
    public class SystemDiagnostics
    {
        /// <summary>
        /// Machine name
        /// </summary>
        public string MachineName { get; set; } = Environment.MachineName;

        /// <summary>
        /// Operating system information
        /// </summary>
        public string OSVersion { get; set; } = Environment.OSVersion.ToString();

        /// <summary>
        /// .NET runtime version
        /// </summary>
        public string RuntimeVersion { get; set; } = Environment.Version.ToString();

        /// <summary>
        /// Processor count
        /// </summary>
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// System page size
        /// </summary>
        public int SystemPageSize { get; set; } = Environment.SystemPageSize;

        /// <summary>
        /// User domain name
        /// </summary>
        public string UserDomainName { get; set; } = Environment.UserDomainName;

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; } = Environment.UserName;

        /// <summary>
        /// Is 64-bit operating system
        /// </summary>
        public bool Is64BitOperatingSystem { get; set; } = Environment.Is64BitOperatingSystem;

        /// <summary>
        /// Is 64-bit process
        /// </summary>
        public bool Is64BitProcess { get; set; } = Environment.Is64BitProcess;

        /// <summary>
        /// System boot time
        /// </summary>
        public DateTime SystemBootTime { get; set; }

        /// <summary>
        /// Available disk space information
        /// </summary>
        public List<DriveInfo> DriveInfo { get; set; } = new();
    }

    /// <summary>
    /// Performance diagnostic information
    /// </summary>
    public class PerformanceDiagnostics
    {
        /// <summary>
        /// Current process information
        /// </summary>
        public ProcessInfo CurrentProcess { get; set; } = new();

        /// <summary>
        /// Memory usage information
        /// </summary>
        public MemoryInfo Memory { get; set; } = new();

        /// <summary>
        /// Garbage collection information
        /// </summary>
        public GCInfo GarbageCollection { get; set; } = new();

        /// <summary>
        /// Thread pool information
        /// </summary>
        public ThreadPoolInfo ThreadPool { get; set; } = new();
    }

    /// <summary>
    /// Process information
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Process ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Process name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Total processor time
        /// </summary>
        public TimeSpan TotalProcessorTime { get; set; }

        /// <summary>
        /// User processor time
        /// </summary>
        public TimeSpan UserProcessorTime { get; set; }

        /// <summary>
        /// Privileged processor time
        /// </summary>
        public TimeSpan PrivilegedProcessorTime { get; set; }

        /// <summary>
        /// Working set memory
        /// </summary>
        public long WorkingSet { get; set; }

        /// <summary>
        /// Peak working set
        /// </summary>
        public long PeakWorkingSet { get; set; }

        /// <summary>
        /// Private memory size
        /// </summary>
        public long PrivateMemorySize { get; set; }

        /// <summary>
        /// Virtual memory size
        /// </summary>
        public long VirtualMemorySize { get; set; }

        /// <summary>
        /// Thread count
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Handle count
        /// </summary>
        public int HandleCount { get; set; }
    }

    /// <summary>
    /// Memory information
    /// </summary>
    public class MemoryInfo
    {
        /// <summary>
        /// Total allocated bytes
        /// </summary>
        public long TotalAllocatedBytes { get; set; }

        /// <summary>
        /// Total memory
        /// </summary>
        public long TotalMemory { get; set; }

        /// <summary>
        /// Available memory
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// Memory usage percentage
        /// </summary>
        public double MemoryUsagePercent { get; set; }
    }

    /// <summary>
    /// Garbage collection information
    /// </summary>
    public class GCInfo
    {
        /// <summary>
        /// Generation 0 collections
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Generation 1 collections
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Generation 2 collections
        /// </summary>
        public int Gen2Collections { get; set; }

        /// <summary>
        /// Total memory before GC
        /// </summary>
        public long TotalMemoryBeforeGC { get; set; }

        /// <summary>
        /// Total memory after GC
        /// </summary>
        public long TotalMemoryAfterGC { get; set; }

        /// <summary>
        /// Maximum generation
        /// </summary>
        public int MaxGeneration { get; set; }
    }

    /// <summary>
    /// Thread pool information
    /// </summary>
    public class ThreadPoolInfo
    {
        /// <summary>
        /// Available worker threads
        /// </summary>
        public int AvailableWorkerThreads { get; set; }

        /// <summary>
        /// Available completion port threads
        /// </summary>
        public int AvailableCompletionPortThreads { get; set; }

        /// <summary>
        /// Maximum worker threads
        /// </summary>
        public int MaxWorkerThreads { get; set; }

        /// <summary>
        /// Maximum completion port threads
        /// </summary>
        public int MaxCompletionPortThreads { get; set; }

        /// <summary>
        /// Minimum worker threads
        /// </summary>
        public int MinWorkerThreads { get; set; }

        /// <summary>
        /// Minimum completion port threads
        /// </summary>
        public int MinCompletionPortThreads { get; set; }
    }

    /// <summary>
    /// Configuration diagnostic information
    /// </summary>
    public class ConfigurationDiagnostics
    {
        /// <summary>
        /// Configuration sources
        /// </summary>
        public List<string> ConfigurationSources { get; set; } = new();

        /// <summary>
        /// Active configuration values (sensitive values filtered)
        /// </summary>
        public Dictionary<string, string> ConfigurationValues { get; set; } = new();

        /// <summary>
        /// Configuration validation results
        /// </summary>
        public List<ConfigurationValidationResult> ValidationResults { get; set; } = new();
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Agent diagnostic information
    /// </summary>
    public class AgentDiagnostics
    {
        /// <summary>
        /// Agent name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Agent type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Current status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Last activity time
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Number of processed requests
        /// </summary>
        public int ProcessedRequests { get; set; }

        /// <summary>
        /// Number of failed requests
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// Average response time
        /// </summary>
        public TimeSpan AverageResponseTime { get; set; }

        /// <summary>
        /// Current queue size
        /// </summary>
        public int QueueSize { get; set; }

        /// <summary>
        /// Configuration information
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// Recent errors
        /// </summary>
        public List<string> RecentErrors { get; set; } = new();
    }

    /// <summary>
    /// Error diagnostic information
    /// </summary>
    public class ErrorDiagnostics
    {
        /// <summary>
        /// Total error count in the last hour
        /// </summary>
        public int ErrorsLastHour { get; set; }

        /// <summary>
        /// Total error count in the last day
        /// </summary>
        public int ErrorsLastDay { get; set; }

        /// <summary>
        /// Most recent errors
        /// </summary>
        public List<RecentErrorSummary> RecentErrors { get; set; } = new();

        /// <summary>
        /// Error rate trend
        /// </summary>
        public TrendDirection ErrorRateTrend { get; set; }
    }

    /// <summary>
    /// Recent error summary
    /// </summary>
    public class RecentErrorSummary
    {
        /// <summary>
        /// When the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Error severity
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Component where the error occurred
        /// </summary>
        public string? Component { get; set; }
    }

    /// <summary>
    /// Assembly information
    /// </summary>
    public class AssemblyInfo
    {
        /// <summary>
        /// Assembly name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Assembly version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Assembly location
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Whether the assembly is in the GAC
        /// </summary>
        public bool IsInGAC { get; set; }

        /// <summary>
        /// Assembly file version
        /// </summary>
        public string? FileVersion { get; set; }
    }

    /// <summary>
    /// Network diagnostic information
    /// </summary>
    public class NetworkDiagnostics
    {
        /// <summary>
        /// Whether the machine is connected to a network
        /// </summary>
        public bool IsNetworkAvailable { get; set; }

        /// <summary>
        /// Network interface information
        /// </summary>
        public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; } = new();

        /// <summary>
        /// DNS servers
        /// </summary>
        public List<string> DnsServers { get; set; } = new();

        /// <summary>
        /// Default gateway
        /// </summary>
        public string? DefaultGateway { get; set; }
    }

    /// <summary>
    /// Network interface information
    /// </summary>
    public class NetworkInterfaceInfo
    {
        /// <summary>
        /// Interface name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Interface type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Operational status
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// IP addresses
        /// </summary>
        public List<string> IPAddresses { get; set; } = new();

        /// <summary>
        /// MAC address
        /// </summary>
        public string? MacAddress { get; set; }
    }

    /// <summary>
    /// Error analysis results
    /// </summary>
    public class ErrorAnalysis
    {
        /// <summary>
        /// Analysis period
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time for analysis
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Key insights from the analysis
        /// </summary>
        public List<string> KeyInsights { get; set; } = new();

        /// <summary>
        /// Identified error patterns
        /// </summary>
        public List<ErrorPattern> ErrorPatterns { get; set; } = new();

        /// <summary>
        /// Recommendations for improvement
        /// </summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// Risk assessment
        /// </summary>
        public RiskAssessment RiskAssessment { get; set; } = new();
    }

    /// <summary>
    /// Identified error pattern
    /// </summary>
    public class ErrorPattern
    {
        /// <summary>
        /// Pattern description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Frequency of the pattern
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Confidence level in the pattern
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Components affected by this pattern
        /// </summary>
        public List<string> AffectedComponents { get; set; } = new();

        /// <summary>
        /// Potential root causes
        /// </summary>
        public List<string> PotentialCauses { get; set; } = new();
    }

    /// <summary>
    /// Risk assessment results
    /// </summary>
    public class RiskAssessment
    {
        /// <summary>
        /// Overall risk level
        /// </summary>
        public RiskLevel OverallRisk { get; set; }

        /// <summary>
        /// Risk factors
        /// </summary>
        public List<RiskFactor> RiskFactors { get; set; } = new();

        /// <summary>
        /// Mitigation suggestions
        /// </summary>
        public List<string> MitigationSuggestions { get; set; } = new();
    }

    /// <summary>
    /// Risk levels
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Risk factor
    /// </summary>
    public class RiskFactor
    {
        /// <summary>
        /// Risk factor name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Risk level
        /// </summary>
        public RiskLevel Level { get; set; }

        /// <summary>
        /// Description of the risk
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Impact if the risk materializes
        /// </summary>
        public string Impact { get; set; } = string.Empty;
    }
}