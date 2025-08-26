using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using System.Collections.Concurrent;

namespace A3sist.Orchastrator.Agents.Dispatcher
{
    internal class FailureHandler : IDisposable
    {
        private readonly ILogger<FailureHandler> _logger;
        private readonly ConcurrentDictionary<string, FailureContext> _activeFailures;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public FailureHandler(ILogger<FailureHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeFailures = new ConcurrentDictionary<string, FailureContext>();
            _cleanupTimer = new Timer(CleanupExpiredFailures, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        internal async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing FailureHandler");
            await Task.CompletedTask;
        }

        internal async Task ShutdownAsync()
        {
            _logger.LogInformation("Shutting down FailureHandler");
            _cleanupTimer?.Dispose();
            _activeFailures.Clear();
            await Task.CompletedTask;
        }

        internal async Task<bool> HandleFailureAsync(string agentName, Exception exception, string context)
        {
            try
            {
                var failureId = Guid.NewGuid().ToString();
                var failureContext = new FailureContext
                {
                    AgentName = agentName,
                    Exception = exception,
                    Context = context,
                    Timestamp = DateTime.UtcNow,
                    RetryCount = 0
                };

                _activeFailures[failureId] = failureContext;
                _logger.LogError(exception, "Handling failure for agent {AgentName} with context {Context}", agentName, context);
                
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling failure for agent {AgentName}", agentName);
                return false;
            }
        }

        private void CleanupExpiredFailures(object? state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-1);
                var expiredFailures = _activeFailures
                    .Where(kvp => kvp.Value.Timestamp < cutoffTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var failureId in expiredFailures)
                {
                    _activeFailures.TryRemove(failureId, out _);
                }

                if (expiredFailures.Any())
                {
                    _logger.LogDebug("Cleaned up {Count} expired failure entries", expiredFailures.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during failure cleanup");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _activeFailures.Clear();
                _disposed = true;
            }
        }
    }

    internal class FailureContext
    {
        public string AgentName { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string Context { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int RetryCount { get; set; }
    }
}