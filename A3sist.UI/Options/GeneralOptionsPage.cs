using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace A3sist.UI.Options;

/// <summary>
/// General options page for A3sist configuration
/// </summary>
[ComVisible(true)]
[Guid("12345678-1234-1234-1234-123456789012")]
public class GeneralOptionsPage : BaseOptionsPage
{
    public override string CategoryName => "A3sist";
    public override string PageName => "General";

    [Category("General")]
    [DisplayName("Enable A3sist")]
    [Description("Enable or disable the A3sist extension")]
    public bool EnableA3sist { get; set; } = true;

    [Category("General")]
    [DisplayName("Auto-start on VS startup")]
    [Description("Automatically start A3sist when Visual Studio starts")]
    public bool AutoStartOnVSStartup { get; set; } = true;

    [Category("General")]
    [DisplayName("Show notifications")]
    [Description("Show notifications for A3sist operations")]
    public bool ShowNotifications { get; set; } = true;

    [Category("General")]
    [DisplayName("Enable telemetry")]
    [Description("Enable anonymous telemetry data collection to improve A3sist")]
    public bool EnableTelemetry { get; set; } = false;

    [Category("Performance")]
    [DisplayName("Max concurrent tasks")]
    [Description("Maximum number of concurrent tasks that can be executed")]
    public int MaxConcurrentTasks { get; set; } = 5;

    [Category("Performance")]
    [DisplayName("Task timeout (seconds)")]
    [Description("Timeout for individual tasks in seconds")]
    public int TaskTimeoutSeconds { get; set; } = 300;

    [Category("Performance")]
    [DisplayName("Enable caching")]
    [Description("Enable caching of results to improve performance")]
    public bool EnableCaching { get; set; } = true;

    [Category("Performance")]
    [DisplayName("Cache expiry (minutes)")]
    [Description("Cache expiry time in minutes")]
    public int CacheExpiryMinutes { get; set; } = 60;

    [Category("Logging")]
    [DisplayName("Log level")]
    [Description("Minimum log level for A3sist logging")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [Category("Logging")]
    [DisplayName("Enable file logging")]
    [Description("Enable logging to files")]
    public bool EnableFileLogging { get; set; } = true;

    [Category("Logging")]
    [DisplayName("Log file path")]
    [Description("Path where log files are stored")]
    public string LogFilePath { get; set; } = "%TEMP%\\A3sist\\logs";

    [Category("Logging")]
    [DisplayName("Max log file size (MB)")]
    [Description("Maximum size of individual log files in MB")]
    public int MaxLogFileSizeMB { get; set; } = 10;

    [Category("Logging")]
    [DisplayName("Max log files")]
    [Description("Maximum number of log files to keep")]
    public int MaxLogFiles { get; set; } = 10;

    public override bool ValidateSettings()
    {
        if (MaxConcurrentTasks < 1 || MaxConcurrentTasks > 100)
        {
            return false;
        }

        if (TaskTimeoutSeconds < 10 || TaskTimeoutSeconds > 3600)
        {
            return false;
        }

        if (CacheExpiryMinutes < 1 || CacheExpiryMinutes > 1440)
        {
            return false;
        }

        if (MaxLogFileSizeMB < 1 || MaxLogFileSizeMB > 1000)
        {
            return false;
        }

        if (MaxLogFiles < 1 || MaxLogFiles > 100)
        {
            return false;
        }

        return true;
    }

    public override void ResetToDefaults()
    {
        EnableA3sist = true;
        AutoStartOnVSStartup = true;
        ShowNotifications = true;
        EnableTelemetry = false;
        MaxConcurrentTasks = 5;
        TaskTimeoutSeconds = 300;
        EnableCaching = true;
        CacheExpiryMinutes = 60;
        LogLevel = LogLevel.Information;
        EnableFileLogging = true;
        LogFilePath = "%TEMP%\\A3sist\\logs";
        MaxLogFileSizeMB = 10;
        MaxLogFiles = 10;
    }
}

/// <summary>
/// Log levels for A3sist logging
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}