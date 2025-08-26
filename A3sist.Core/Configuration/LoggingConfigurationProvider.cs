using A3sist.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace A3sist.Core.Configuration
{
    /// <summary>
    /// Provider for loading logging configuration from various sources
    /// </summary>
    public class LoggingConfigurationProvider
    {
        private readonly IConfiguration _configuration;

        public LoggingConfigurationProvider(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Loads logging configuration from configuration sources
        /// </summary>
        /// <returns>The logging configuration</returns>
        public LoggingConfiguration LoadConfiguration()
        {
            var config = new LoggingConfiguration();

            // Load from configuration section
            var loggingSection = _configuration.GetSection("A3sist:Logging");
            if (loggingSection.Exists())
            {
                loggingSection.Bind(config);
            }

            // Override with environment variables if present
            LoadFromEnvironmentVariables(config);

            // Validate and apply defaults
            ValidateAndApplyDefaults(config);

            return config;
        }

        /// <summary>
        /// Creates a default logging configuration
        /// </summary>
        /// <returns>Default logging configuration</returns>
        public static LoggingConfiguration CreateDefault()
        {
            var config = new LoggingConfiguration();
            ValidateAndApplyDefaults(config);
            return config;
        }

        private void LoadFromEnvironmentVariables(LoggingConfiguration config)
        {
            // Check for environment variable overrides
            var logLevel = Environment.GetEnvironmentVariable("A3SIST_LOG_LEVEL");
            if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse<LogLevel>(logLevel, true, out var level))
            {
                config.MinimumLevel = level;
            }

            var logPath = Environment.GetEnvironmentVariable("A3SIST_LOG_PATH");
            if (!string.IsNullOrEmpty(logPath))
            {
                config.LogFilePath = logPath;
            }

            var maxFileSize = Environment.GetEnvironmentVariable("A3SIST_LOG_MAX_FILE_SIZE_MB");
            if (!string.IsNullOrEmpty(maxFileSize) && int.TryParse(maxFileSize, out var size))
            {
                config.MaxFileSizeMB = size;
            }

            var retainedFiles = Environment.GetEnvironmentVariable("A3SIST_LOG_RETAINED_FILES");
            if (!string.IsNullOrEmpty(retainedFiles) && int.TryParse(retainedFiles, out var count))
            {
                config.RetainedFileCountLimit = count;
            }

            var writeToConsole = Environment.GetEnvironmentVariable("A3SIST_LOG_CONSOLE");
            if (!string.IsNullOrEmpty(writeToConsole) && bool.TryParse(writeToConsole, out var console))
            {
                config.WriteToConsole = console;
            }

            var writeToFile = Environment.GetEnvironmentVariable("A3SIST_LOG_FILE");
            if (!string.IsNullOrEmpty(writeToFile) && bool.TryParse(writeToFile, out var file))
            {
                config.WriteToFile = file;
            }
        }

        private static void ValidateAndApplyDefaults(LoggingConfiguration config)
        {
            // Ensure log path is valid
            if (string.IsNullOrWhiteSpace(config.LogFilePath))
            {
                config.LogFilePath = Path.Combine(Path.GetTempPath(), "A3sist", "logs");
            }

            // Ensure reasonable file size limits
            if (config.MaxFileSizeMB <= 0)
            {
                config.MaxFileSizeMB = 10;
            }
            else if (config.MaxFileSizeMB > 100)
            {
                config.MaxFileSizeMB = 100; // Cap at 100MB
            }

            // Ensure reasonable file retention
            if (config.RetainedFileCountLimit <= 0)
            {
                config.RetainedFileCountLimit = 10;
            }
            else if (config.RetainedFileCountLimit > 100)
            {
                config.RetainedFileCountLimit = 100; // Cap at 100 files
            }

            // Ensure output template is not empty
            if (string.IsNullOrWhiteSpace(config.OutputTemplate))
            {
                config.OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
            }

            // Initialize collections if null
            config.LogLevelOverrides ??= new Dictionary<string, LogLevel>();
            config.GlobalProperties ??= new Dictionary<string, object>();

            // Add default global properties
            if (!config.GlobalProperties.ContainsKey("Application"))
            {
                config.GlobalProperties["Application"] = "A3sist";
            }

            if (!config.GlobalProperties.ContainsKey("Version"))
            {
                config.GlobalProperties["Version"] = GetAssemblyVersion();
            }
        }

        private static string GetAssemblyVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                return assembly.GetName().Version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}