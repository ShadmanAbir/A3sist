using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Core.Configuration;
using A3sist.Core.Agents.Base;
using A3sist.Shared.Models;
using A3sist.Shared.Interfaces;

namespace A3sist.Core.Services
{


    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long TotalMemoryUsage { get; set; }
        public double CpuUsagePercent { get; set; }
        public int ActiveAgents { get; set; }
        public int TotalRequestsProcessed { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double CacheHitRatio { get; set; }
        public ConcurrentDictionary<string, AgentMetrics> AgentMetrics { get; set; } = new();
    }

    /// <summary>
    /// Agent-specific performance report
    /// </summary>
    public class AgentPerformanceReport
    {
        public string AgentName { get; set; } = string.Empty;
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MinExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0.0;
        public DateTime LastExecution { get; set; }
        public long TotalMemoryUsed { get; set; }
        public int CurrentLoad { get; set; }
    }

    /// <summary>
    /// Enhanced performance monitoring service with detailed metrics collection
    /// </summary>
    public class EnhancedPerformanceMonitoringService : IPerformanceMonitoringService, IDisposable
    {
        private readonly ILogger<EnhancedPerformanceMonitoringService> _logger;
        private readonly A3sistOptions _options;
        private readonly Timer _metricsTimer;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _memoryCounter;
        private readonly ConcurrentDictionary<string, AgentMetrics> _agentMetrics;
        private readonly ConcurrentDictionary<string, OperationTracker> _activeOperations;
        private readonly ConcurrentDictionary<string, long> _cacheStats;
        
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _totalCacheHits;
        private long _totalCacheMisses;
        private bool _disposed;

        public EnhancedPerformanceMonitoringService(
            ILogger<EnhancedPerformanceMonitoringService> logger,
            IOptions<A3sistOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _agentMetrics = new ConcurrentDictionary<string, AgentMetrics>();
            _activeOperations = new ConcurrentDictionary<string, OperationTracker>();
            _cacheStats = new ConcurrentDictionary<string, long>();

            // Initialize performance counters (Windows-specific)
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Performance counters not available, using alternative methods");
            }

            // Start metrics collection timer if monitoring is enabled
            if (_options.Performance.EnableMonitoring)
            {
                _metricsTimer = new Timer(CollectMetrics, null, 
                    TimeSpan.Zero, _options.Performance.MetricsInterval);
                _logger.LogInformation("Performance monitoring started with {Interval} interval", 
                    _options.Performance.MetricsInterval);
            }
        }

        /// <summary>
        /// Records agent execution metrics
        /// </summary>
        public void RecordAgentExecution(string agentName, TimeSpan duration, bool success)
        {
            if (string.IsNullOrWhiteSpace(agentName))
                return;

            Interlocked.Increment(ref _totalRequests);
            if (success)
                Interlocked.Increment(ref _successfulRequests);
            else
                Interlocked.Increment(ref _failedRequests);

            _agentMetrics.AddOrUpdate(agentName, 
                new AgentMetrics
                {
                    Name = agentName,
                    TasksProcessed = 1,
                    TasksSucceeded = success ? 1 : 0,
                    TasksFailed = success ? 0 : 1,
                    TotalExecutionTime = duration,
                    MinExecutionTime = duration,
                    MaxExecutionTime = duration,
                    LastActivity = DateTime.UtcNow
                },
                (key, existing) =>
                {
                    existing.TasksProcessed++;
                    if (success)
                        existing.TasksSucceeded++;
                    else
                        existing.TasksFailed++;
                    
                    existing.TotalExecutionTime = existing.TotalExecutionTime.Add(duration);
                    existing.MinExecutionTime = duration < existing.MinExecutionTime ? duration : existing.MinExecutionTime;
                    existing.MaxExecutionTime = duration > existing.MaxExecutionTime ? duration : existing.MaxExecutionTime;
                    existing.LastActivity = DateTime.UtcNow;
                    return existing;
                });

            _logger.LogTrace("Recorded execution for agent {AgentName}: {Duration}ms, Success: {Success}", 
                agentName, duration.TotalMilliseconds, success);
        }

        /// <summary>
        /// Records memory usage
        /// </summary>
        public void RecordMemoryUsage(long memoryBytes)
        {
            // Implementation for memory usage tracking
            _logger.LogTrace("Memory usage recorded: {MemoryMB}MB", memoryBytes / 1024 / 1024);
        }

        /// <summary>
        /// Records cache hit
        /// </summary>
        public void RecordCacheHit(string cacheKey)
        {
            Interlocked.Increment(ref _totalCacheHits);
            _cacheStats.AddOrUpdate("hits", 1, (key, value) => value + 1);
            _logger.LogTrace("Cache hit recorded for key: {CacheKey}", cacheKey);
        }

        /// <summary>
        /// Records cache miss
        /// </summary>
        public void RecordCacheMiss(string cacheKey)
        {
            Interlocked.Increment(ref _totalCacheMisses);
            _cacheStats.AddOrUpdate("misses", 1, (key, value) => value + 1);
            _logger.LogTrace("Cache miss recorded for key: {CacheKey}", cacheKey);
        }

        /// <summary>
        /// Gets comprehensive performance metrics
        /// </summary>
        public async Task<PerformanceMetrics> GetMetricsAsync()
        {
            await Task.CompletedTask; // For async consistency

            var totalMemory = GC.GetTotalMemory(false);
            var cpuUsage = GetCpuUsage();
            var totalCacheRequests = _totalCacheHits + _totalCacheMisses;
            var cacheHitRatio = totalCacheRequests > 0 ? (double)_totalCacheHits / totalCacheRequests : 0.0;

            var avgResponseTime = CalculateAverageResponseTime();

            return new PerformanceMetrics
            {
                TotalMemoryUsage = totalMemory,
                CpuUsagePercent = cpuUsage,
                ActiveAgents = _agentMetrics.Count,
                TotalRequestsProcessed = (int)_totalRequests,
                SuccessfulRequests = (int)_successfulRequests,
                FailedRequests = (int)_failedRequests,
                AverageResponseTime = avgResponseTime,
                CacheHitRatio = cacheHitRatio,
                AgentMetrics = new ConcurrentDictionary<string, AgentMetrics>(_agentMetrics)
            };
        }

        /// <summary>
        /// Gets performance report for a specific agent
        /// </summary>
        public async Task<AgentPerformanceReport> GetAgentReportAsync(string agentName)
        {
            await Task.CompletedTask; // For async consistency

            if (!_agentMetrics.TryGetValue(agentName, out var metrics))
            {
                return new AgentPerformanceReport { AgentName = agentName };
            }

            var avgExecutionTime = metrics.TasksProcessed > 0 
                ? TimeSpan.FromTicks(metrics.TotalExecutionTime.Ticks / metrics.TasksProcessed)
                : TimeSpan.Zero;

            return new AgentPerformanceReport
            {
                AgentName = agentName,
                TotalExecutions = metrics.TasksProcessed,
                SuccessfulExecutions = metrics.TasksSucceeded,
                FailedExecutions = metrics.TasksFailed,
                AverageExecutionTime = avgExecutionTime,
                MinExecutionTime = metrics.MinExecutionTime,
                MaxExecutionTime = metrics.MaxExecutionTime,
                LastExecution = metrics.LastActivity,
                CurrentLoad = 0 // Could be enhanced to track current load
            };
        }

        /// <summary>
        /// Starts tracking an operation
        /// </summary>
        public void StartOperation(string operationName)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                return;

            var tracker = new OperationTracker
            {
                Name = operationName,
                StartTime = DateTime.UtcNow,
                Stopwatch = Stopwatch.StartNew()
            };

            _activeOperations.TryAdd(operationName, tracker);
            _logger.LogTrace("Started tracking operation: {OperationName}", operationName);
        }

        /// <summary>
        /// Ends tracking an operation
        /// </summary>
        public void EndOperation(string operationName, bool success = true)
        {
            if (string.IsNullOrWhiteSpace(operationName))
                return;

            if (_activeOperations.TryRemove(operationName, out var tracker))
            {
                tracker.Stopwatch.Stop();
                var duration = tracker.Stopwatch.Elapsed;

                _logger.LogInformation("Operation {OperationName} completed in {Duration}ms with success: {Success}",
                    operationName, duration.TotalMilliseconds, success);

                // Record as a generic agent execution for aggregated metrics
                RecordAgentExecution("System", duration, success);
            }
        }

        /// <summary>
        /// Collects system metrics periodically
        /// </summary>
        private async void CollectMetrics(object? state)
        {
            if (_disposed)
                return;

            try
            {
                var metrics = await GetMetricsAsync();
                
                _logger.LogInformation(
                    "Performance Metrics - Memory: {MemoryMB}MB, CPU: {CpuPercent:F1}%, " +
                    "Active Agents: {ActiveAgents}, Requests: {TotalRequests} " +
                    "(Success: {SuccessRate:F1}%), Cache Hit Ratio: {CacheHitRatio:F1}%",
                    metrics.TotalMemoryUsage / 1024 / 1024,
                    metrics.CpuUsagePercent,
                    metrics.ActiveAgents,
                    metrics.TotalRequestsProcessed,
                    metrics.TotalRequestsProcessed > 0 ? (double)metrics.SuccessfulRequests / metrics.TotalRequestsProcessed * 100 : 0,
                    metrics.CacheHitRatio * 100);

                // Check for performance issues
                if (metrics.TotalMemoryUsage > _options.Performance.MaxMemoryUsageMB * 1024 * 1024)
                {
                    _logger.LogWarning("Memory usage ({MemoryMB}MB) exceeds threshold ({ThresholdMB}MB)",
                        metrics.TotalMemoryUsage / 1024 / 1024, _options.Performance.MaxMemoryUsageMB);
                }

                if (metrics.CpuUsagePercent > 80)
                {
                    _logger.LogWarning("High CPU usage detected: {CpuPercent:F1}%", metrics.CpuUsagePercent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting performance metrics");
            }
        }

        /// <summary>
        /// Gets current CPU usage
        /// </summary>
        private double GetCpuUsage()
        {
            try
            {
                if (_cpuCounter != null)
                {
                    return _cpuCounter.NextValue();
                }
                
                // Fallback method using Process
                using var process = Process.GetCurrentProcess();
                return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / Environment.TickCount * 100;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Error getting CPU usage");
                return 0.0;
            }
        }

        /// <summary>
        /// Calculates average response time across all agents
        /// </summary>
        private TimeSpan CalculateAverageResponseTime()
        {
            if (_agentMetrics.IsEmpty)
                return TimeSpan.Zero;

            long totalTicks = 0;
            int totalRequests = 0;

            foreach (var metrics in _agentMetrics.Values)
            {
                totalTicks += metrics.TotalExecutionTime.Ticks;
                totalRequests += metrics.TasksProcessed;
            }

            return totalRequests > 0 ? TimeSpan.FromTicks(totalTicks / totalRequests) : TimeSpan.Zero;
        }

        // Shared interface implementation methods
        public async Task RecordMetricAsync(A3sist.Shared.Models.PerformanceMetric metric)
        {
            await Task.CompletedTask;
            if (metric.Name.Contains("agent"))
            {
                RecordAgentExecution(metric.Source ?? "Unknown", TimeSpan.FromMilliseconds(metric.Value), true);
            }
        }

        public async Task RecordCounterAsync(string name, double value = 1, Dictionary<string, string>? tags = null)
        {
            await Task.CompletedTask;
            _logger.LogTrace("Counter {Name} recorded: {Value}", name, value);
        }

        public async Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? tags = null)
        {
            await Task.CompletedTask;
            if (name.ToLower().Contains("memory"))
            {
                RecordMemoryUsage((long)value);
            }
        }

        public async Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? tags = null)
        {
            await Task.CompletedTask;
            _logger.LogTrace("Histogram {Name} recorded: {Value}", name, value);
        }

        public async Task RecordTimingAsync(string name, TimeSpan duration, Dictionary<string, string>? tags = null)
        {
            await Task.CompletedTask;
            RecordAgentExecution(tags?.GetValueOrDefault("agent") ?? "System", duration, true);
        }

        public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
        {
            return new SharedMetricTimer(name, this);
        }

        public async Task<IEnumerable<A3sist.Shared.Models.PerformanceMetric>> GetMetricsAsync(string? metricName = null, DateTime? startTime = null, DateTime? endTime = null, Dictionary<string, string>? tags = null)
        {
            await Task.CompletedTask;
            var metrics = new List<A3sist.Shared.Models.PerformanceMetric>();
            
            foreach (var agent in _agentMetrics)
            {
                metrics.Add(new A3sist.Shared.Models.PerformanceMetric
                {
                    Name = $"agent.{agent.Key}.execution_time",
                    Type = MetricType.Timer,
                    Value = agent.Value.TotalExecutionTime.TotalMilliseconds,
                    Unit = "ms",
                    Timestamp = agent.Value.LastActivity,
                    Source = agent.Key
                });
            }
            
            return metrics;
        }

        public async Task<A3sist.Shared.Models.PerformanceStatistics> GetStatisticsAsync(string metricName, DateTime? startTime = null, DateTime? endTime = null, Dictionary<string, string>? tags = null)
        {
            await Task.CompletedTask;
            return new A3sist.Shared.Models.PerformanceStatistics
            {
                MetricName = metricName,
                StartTime = startTime ?? DateTime.UtcNow.AddDays(-1),
                EndTime = endTime ?? DateTime.UtcNow
            };
        }

        public async Task<SystemHealthMetrics> GetSystemHealthAsync()
        {
            var metrics = await GetMetricsAsync();
            return new SystemHealthMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = GetCpuUsage(),
                MemoryUsageBytes = GC.GetTotalMemory(false)
            };
        }

        public async Task CleanupMetricsAsync()
        {
            await Task.CompletedTask;
            var cutoffTime = DateTime.UtcNow.AddDays(-7);
            var expiredAgents = _agentMetrics
                .Where(kvp => kvp.Value.LastActivity < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var agent in expiredAgents)
            {
                _agentMetrics.TryRemove(agent, out _);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _metricsTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            
            _logger.LogInformation("Performance monitoring service disposed");
        }

        /// <summary>
        /// Internal class for tracking active operations
        /// </summary>
        private class OperationTracker
        {
            public string Name { get; set; } = string.Empty;
            public DateTime StartTime { get; set; }
            public Stopwatch Stopwatch { get; set; } = new();
        }
        
        /// <summary>
        /// Timer for shared metrics interface
        /// </summary>
        private class SharedMetricTimer : IDisposable
        {
            private readonly string _name;
            private readonly EnhancedPerformanceMonitoringService _service;
            private readonly Stopwatch _stopwatch;
            
            public SharedMetricTimer(string name, EnhancedPerformanceMonitoringService service)
            {
                _name = name;
                _service = service;
                _stopwatch = Stopwatch.StartNew();
            }
            
            public void Dispose()
            {
                _stopwatch.Stop();
                _service.RecordAgentExecution("Timer", _stopwatch.Elapsed, true);
            }
        }
    }
}