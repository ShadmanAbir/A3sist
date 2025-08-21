using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Service for monitoring and collecting performance metrics
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService, IDisposable
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly ConcurrentQueue<PerformanceMetric> _metricsQueue;
        private readonly ConcurrentDictionary<string, List<PerformanceMetric>> _metricsStorage;
        private readonly Timer _cleanupTimer;
        private readonly Timer _systemHealthTimer;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _memoryCounter;
        private readonly Process _currentProcess;
        private readonly DateTime _startTime;
        private bool _disposed;

        // Configuration
        private readonly TimeSpan _metricsRetentionPeriod = TimeSpan.FromDays(7);
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _systemHealthInterval = TimeSpan.FromMinutes(1);
        private readonly int _maxMetricsPerName = 10000;

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsQueue = new ConcurrentQueue<PerformanceMetric>();
            _metricsStorage = new ConcurrentDictionary<string, List<PerformanceMetric>>();
            _currentProcess = Process.GetCurrentProcess();
            _startTime = DateTime.UtcNow;

            // Initialize performance counters (may fail on some systems)
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters. System metrics may be limited.");
            }

            // Start background timers
            _cleanupTimer = new Timer(async _ => await CleanupMetricsAsync(), null, _cleanupInterval, _cleanupInterval);
            _systemHealthTimer = new Timer(async _ => await CollectSystemHealthMetricsAsync(), null, TimeSpan.Zero, _systemHealthInterval);

            _logger.LogInformation("Performance monitoring service initialized");
        }

        public async Task RecordMetricAsync(PerformanceMetric metric)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PerformanceMonitoringService));

            if (metric == null)
                throw new ArgumentNullException(nameof(metric));

            metric.Timestamp = DateTime.UtcNow;
            metric.Source = metric.Source ?? "A3sist";

            _metricsQueue.Enqueue(metric);
            await ProcessQueuedMetricsAsync();
        }

        public async Task RecordCounterAsync(string name, double value = 1, Dictionary<string, string>? tags = null)
        {
            var metric = new PerformanceMetric
            {
                Name = name,
                Type = MetricType.Counter,
                Value = value,
                Unit = "count",
                Tags = tags ?? new Dictionary<string, string>()
            };

            await RecordMetricAsync(metric);
        }

        public async Task RecordGaugeAsync(string name, double value, Dictionary<string, string>? tags = null)
        {
            var metric = new PerformanceMetric
            {
                Name = name,
                Type = MetricType.Gauge,
                Value = value,
                Tags = tags ?? new Dictionary<string, string>()
            };

            await RecordMetricAsync(metric);
        }

        public async Task RecordHistogramAsync(string name, double value, Dictionary<string, string>? tags = null)
        {
            var metric = new PerformanceMetric
            {
                Name = name,
                Type = MetricType.Histogram,
                Value = value,
                Tags = tags ?? new Dictionary<string, string>()
            };

            await RecordMetricAsync(metric);
        }

        public async Task RecordTimingAsync(string name, TimeSpan duration, Dictionary<string, string>? tags = null)
        {
            var metric = new PerformanceMetric
            {
                Name = name,
                Type = MetricType.Timer,
                Value = duration.TotalMilliseconds,
                Unit = "ms",
                Tags = tags ?? new Dictionary<string, string>()
            };

            await RecordMetricAsync(metric);
        }

        public IDisposable StartTimer(string name, Dictionary<string, string>? tags = null)
        {
            return new MetricTimer(this, name, tags);
        }

        public async Task<IEnumerable<PerformanceMetric>> GetMetricsAsync(string? metricName = null, 
            DateTime? startTime = null, DateTime? endTime = null, Dictionary<string, string>? tags = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PerformanceMonitoringService));

            await ProcessQueuedMetricsAsync();

            var allMetrics = new List<PerformanceMetric>();

            if (metricName != null)
            {
                if (_metricsStorage.TryGetValue(metricName, out var metrics))
                {
                    allMetrics.AddRange(metrics);
                }
            }
            else
            {
                foreach (var kvp in _metricsStorage)
                {
                    allMetrics.AddRange(kvp.Value);
                }
            }

            // Apply filters
            var filteredMetrics = allMetrics.AsEnumerable();

            if (startTime.HasValue)
            {
                filteredMetrics = filteredMetrics.Where(m => m.Timestamp >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                filteredMetrics = filteredMetrics.Where(m => m.Timestamp <= endTime.Value);
            }

            if (tags != null && tags.Any())
            {
                filteredMetrics = filteredMetrics.Where(m => 
                    tags.All(tag => m.Tags.ContainsKey(tag.Key) && m.Tags[tag.Key] == tag.Value));
            }

            return filteredMetrics.OrderBy(m => m.Timestamp);
        }

        public async Task<PerformanceStatistics> GetStatisticsAsync(string metricName, 
            DateTime? startTime = null, DateTime? endTime = null, Dictionary<string, string>? tags = null)
        {
            var metrics = await GetMetricsAsync(metricName, startTime, endTime, tags);
            var values = metrics.Select(m => m.Value).ToList();

            if (!values.Any())
            {
                return new PerformanceStatistics
                {
                    MetricName = metricName,
                    StartTime = startTime ?? DateTime.MinValue,
                    EndTime = endTime ?? DateTime.MaxValue,
                    FilterTags = tags ?? new Dictionary<string, string>()
                };
            }

            values.Sort();
            var count = values.Count;
            var sum = values.Sum();
            var average = sum / count;
            var min = values.First();
            var max = values.Last();
            var median = count % 2 == 0 ? (values[count / 2 - 1] + values[count / 2]) / 2 : values[count / 2];
            var p95 = values[(int)(count * 0.95)];
            var p99 = values[(int)(count * 0.99)];
            var variance = values.Sum(v => Math.Pow(v - average, 2)) / count;
            var standardDeviation = Math.Sqrt(variance);

            var timeRange = endTime ?? DateTime.UtcNow - (startTime ?? metrics.Min(m => m.Timestamp));
            var rate = timeRange.TotalSeconds > 0 ? sum / timeRange.TotalSeconds : 0;

            return new PerformanceStatistics
            {
                MetricName = metricName,
                Count = count,
                Min = min,
                Max = max,
                Average = average,
                Median = median,
                P95 = p95,
                P99 = p99,
                StandardDeviation = standardDeviation,
                Sum = sum,
                Rate = rate,
                StartTime = startTime ?? metrics.Min(m => m.Timestamp),
                EndTime = endTime ?? metrics.Max(m => m.Timestamp),
                Unit = metrics.FirstOrDefault()?.Unit ?? "",
                FilterTags = tags ?? new Dictionary<string, string>()
            };
        }

        public async Task<SystemHealthMetrics> GetSystemHealthAsync()
        {
            var healthMetrics = new SystemHealthMetrics();

            try
            {
                // CPU metrics
                if (_cpuCounter != null)
                {
                    healthMetrics.Cpu.UsagePercent = _cpuCounter.NextValue();
                }
                healthMetrics.Cpu.CoreCount = Environment.ProcessorCount;

                // Memory metrics
                var totalMemory = GC.GetTotalMemory(false);
                healthMetrics.Memory.WorkingSetBytes = _currentProcess.WorkingSet64;
                healthMetrics.Memory.PrivateMemoryBytes = _currentProcess.PrivateMemorySize64;

                if (_memoryCounter != null)
                {
                    var availableMemoryMB = _memoryCounter.NextValue();
                    healthMetrics.Memory.AvailableBytes = (long)(availableMemoryMB * 1024 * 1024);
                }

                // Disk metrics (for current drive)
                var currentDrive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
                if (currentDrive.IsReady)
                {
                    healthMetrics.Disk.TotalBytes = currentDrive.TotalSize;
                    healthMetrics.Disk.AvailableBytes = currentDrive.AvailableFreeSpace;
                    healthMetrics.Disk.UsedBytes = currentDrive.TotalSize - currentDrive.AvailableFreeSpace;
                    healthMetrics.Disk.UsagePercent = (double)healthMetrics.Disk.UsedBytes / healthMetrics.Disk.TotalBytes * 100;
                }

                // Application metrics
                healthMetrics.Application.Uptime = DateTime.UtcNow - _startTime;
                healthMetrics.Application.ThreadCount = _currentProcess.Threads.Count;
                healthMetrics.Application.AssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;
                healthMetrics.Application.Gen0Collections = GC.CollectionCount(0);
                healthMetrics.Application.Gen1Collections = GC.CollectionCount(1);
                healthMetrics.Application.Gen2Collections = GC.CollectionCount(2);
                healthMetrics.Application.TotalAllocatedBytes = GC.GetTotalAllocatedBytes();

                // Calculate memory usage percentage
                if (healthMetrics.Memory.AvailableBytes > 0)
                {
                    var totalSystemMemory = healthMetrics.Memory.AvailableBytes + healthMetrics.Memory.WorkingSetBytes;
                    healthMetrics.Memory.TotalBytes = totalSystemMemory;
                    healthMetrics.Memory.UsedBytes = healthMetrics.Memory.WorkingSetBytes;
                    healthMetrics.Memory.UsagePercent = (double)healthMetrics.Memory.UsedBytes / healthMetrics.Memory.TotalBytes * 100;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect some system health metrics");
            }

            return healthMetrics;
        }

        public async Task CleanupMetricsAsync()
        {
            if (_disposed)
                return;

            try
            {
                var cutoffTime = DateTime.UtcNow - _metricsRetentionPeriod;
                var removedCount = 0;

                foreach (var kvp in _metricsStorage.ToList())
                {
                    var metrics = kvp.Value;
                    var originalCount = metrics.Count;
                    
                    // Remove old metrics
                    metrics.RemoveAll(m => m.Timestamp < cutoffTime);
                    
                    // Limit the number of metrics per name
                    if (metrics.Count > _maxMetricsPerName)
                    {
                        metrics.RemoveRange(0, metrics.Count - _maxMetricsPerName);
                    }

                    removedCount += originalCount - metrics.Count;

                    // Remove empty entries
                    if (!metrics.Any())
                    {
                        _metricsStorage.TryRemove(kvp.Key, out _);
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {RemovedCount} old performance metrics", removedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup performance metrics");
            }

            await Task.CompletedTask;
        }

        private async Task ProcessQueuedMetricsAsync()
        {
            while (_metricsQueue.TryDequeue(out var metric))
            {
                var metricsList = _metricsStorage.GetOrAdd(metric.Name, _ => new List<PerformanceMetric>());
                
                lock (metricsList)
                {
                    metricsList.Add(metric);
                    
                    // Limit the number of metrics per name
                    if (metricsList.Count > _maxMetricsPerName)
                    {
                        metricsList.RemoveAt(0);
                    }
                }
            }

            await Task.CompletedTask;
        }

        private async Task CollectSystemHealthMetricsAsync()
        {
            try
            {
                var healthMetrics = await GetSystemHealthAsync();
                
                // Record system health as metrics
                await RecordGaugeAsync("system.cpu.usage_percent", healthMetrics.Cpu.UsagePercent);
                await RecordGaugeAsync("system.memory.usage_percent", healthMetrics.Memory.UsagePercent);
                await RecordGaugeAsync("system.memory.working_set_bytes", healthMetrics.Memory.WorkingSetBytes);
                await RecordGaugeAsync("system.disk.usage_percent", healthMetrics.Disk.UsagePercent);
                await RecordGaugeAsync("application.thread_count", healthMetrics.Application.ThreadCount);
                await RecordGaugeAsync("application.uptime_seconds", healthMetrics.Application.Uptime.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect system health metrics");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _cleanupTimer?.Dispose();
            _systemHealthTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _currentProcess?.Dispose();

            _disposed = true;
            _logger.LogInformation("Performance monitoring service disposed");
        }
    }

    /// <summary>
    /// Timer for measuring operation duration
    /// </summary>
    internal class MetricTimer : IDisposable
    {
        private readonly PerformanceMonitoringService _service;
        private readonly string _name;
        private readonly Dictionary<string, string>? _tags;
        private readonly DateTime _startTime;
        private bool _disposed;

        public MetricTimer(PerformanceMonitoringService service, string name, Dictionary<string, string>? tags)
        {
            _service = service;
            _name = name;
            _tags = tags;
            _startTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var duration = DateTime.UtcNow - _startTime;
            _ = Task.Run(async () => await _service.RecordTimingAsync(_name, duration, _tags));
            
            _disposed = true;
        }
    }
}