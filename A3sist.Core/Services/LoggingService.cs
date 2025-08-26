using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Service for managing application logging with Serilog
    /// </summary>
    public class LoggingService : ILoggingService, IDisposable
    {
        private readonly object _lock = new();
        private LoggingConfiguration _configuration;
        private Logger? _serilogLogger;
        private ILoggerFactory? _loggerFactory;
        private bool _disposed;

        public LoggingService(LoggingConfiguration? configuration = null)
        {
            _configuration = configuration ?? new LoggingConfiguration();
            InitializeLogger();
        }

        public ILogger<T> CreateLogger<T>()
        {
            EnsureNotDisposed();
            return _loggerFactory?.CreateLogger<T>() ?? throw new InvalidOperationException("Logger factory not initialized");
        }

        public ILogger CreateLogger(string categoryName)
        {
            EnsureNotDisposed();
            return _loggerFactory?.CreateLogger(categoryName) ?? throw new InvalidOperationException("Logger factory not initialized");
        }

        public async Task UpdateConfigurationAsync(LoggingConfiguration configuration)
        {
            EnsureNotDisposed();
            
            lock (_lock)
            {
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                
                // Dispose existing logger
                _loggerFactory?.Dispose();
                _serilogLogger?.Dispose();
                
                // Reinitialize with new configuration
                InitializeLogger();
            }
            
            await Task.CompletedTask;
        }

        public LoggingConfiguration GetConfiguration()
        {
            EnsureNotDisposed();
            return _configuration;
        }

        public async Task CleanupLogsAsync()
        {
            EnsureNotDisposed();
            
            try
            {
                if (!Directory.Exists(_configuration.LogFilePath))
                    return;

                var logFiles = Directory.GetFiles(_configuration.LogFilePath, "*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Keep only the specified number of files
                var filesToDelete = logFiles.Skip(_configuration.RetainedFileCountLimit);
                
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        // Log cleanup failure but don't throw
                        _serilogLogger?.Warning(ex, "Failed to delete log file {FilePath}", file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _serilogLogger?.Error(ex, "Failed to cleanup log files");
                throw;
            }
            
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<LogFileInfo>> GetLogFilesAsync()
        {
            EnsureNotDisposed();
            
            var logFiles = new List<LogFileInfo>();
            
            try
            {
                if (!Directory.Exists(_configuration.LogFilePath))
                    return logFiles;

                var files = Directory.GetFiles(_configuration.LogFilePath, "*.log");
                var currentLogFile = GetCurrentLogFileName();
                
                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    logFiles.Add(new LogFileInfo
                    {
                        FilePath = filePath,
                        SizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime,
                        LastModified = fileInfo.LastWriteTime,
                        IsActive = Path.GetFileName(filePath) == currentLogFile
                    });
                }
            }
            catch (Exception ex)
            {
                _serilogLogger?.Error(ex, "Failed to get log file information");
                throw;
            }
            
            return logFiles.OrderByDescending(f => f.CreatedAt);
        }

        private void InitializeLogger()
        {
            try
            {
                // Ensure log directory exists
                if (_configuration.WriteToFile && !Directory.Exists(_configuration.LogFilePath))
                {
                    Directory.CreateDirectory(_configuration.LogFilePath);
                }

                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Is(ConvertLogLevel(_configuration.MinimumLevel))
                    .Enrich.FromLogContext();

                // Add global properties
                foreach (var property in _configuration.GlobalProperties)
                {
                    loggerConfig.Enrich.WithProperty(property.Key, property.Value);
                }

                // Configure console sink
                if (_configuration.WriteToConsole)
                {
                    loggerConfig.WriteTo.Console(
                        outputTemplate: _configuration.OutputTemplate,
                        restrictedToMinimumLevel: ConvertLogLevel(_configuration.MinimumLevel));
                }

                // Configure file sink
                if (_configuration.WriteToFile)
                {
                    var logFilePath = Path.Combine(_configuration.LogFilePath, GetCurrentLogFileName());
                    loggerConfig.WriteTo.File(
                        path: logFilePath,
                        outputTemplate: _configuration.OutputTemplate,
                        restrictedToMinimumLevel: ConvertLogLevel(_configuration.MinimumLevel),
                        fileSizeLimitBytes: _configuration.MaxFileSizeMB * 1024 * 1024,
                        retainedFileCountLimit: _configuration.RetainedFileCountLimit,
                        rollOnFileSizeLimit: true,
                        shared: true);
                }

                // Apply log level overrides
                foreach (var levelOverride in _configuration.LogLevelOverrides)
                {
                    loggerConfig.MinimumLevel.Override(levelOverride.Key, ConvertLogLevel(levelOverride.Value));
                }

                _serilogLogger = loggerConfig.CreateLogger();
                
                // Create Microsoft.Extensions.Logging factory
                _loggerFactory = new SerilogLoggerFactory(_serilogLogger, dispose: false);
            }
            catch (Exception ex)
            {
                // Fallback to console logging if file logging fails
                _serilogLogger = new LoggerConfiguration()
                    .MinimumLevel.Warning()
                    .WriteTo.Console()
                    .CreateLogger();
                
                _loggerFactory = new SerilogLoggerFactory(_serilogLogger, dispose: false);
                
                _serilogLogger.Error(ex, "Failed to initialize logging with configured settings, using fallback configuration");
            }
        }

        private static LogEventLevel ConvertLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => LogEventLevel.Verbose,
                LogLevel.Debug => LogEventLevel.Debug,
                LogLevel.Information => LogEventLevel.Information,
                LogLevel.Warning => LogEventLevel.Warning,
                LogLevel.Error => LogEventLevel.Error,
                LogLevel.Critical => LogEventLevel.Fatal,
                LogLevel.None => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        private string GetCurrentLogFileName()
        {
            return $"a3sist-{DateTime.Now:yyyyMMdd}.log";
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LoggingService));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _loggerFactory?.Dispose();
            _serilogLogger?.Dispose();
            _disposed = true;
        }
    }
}