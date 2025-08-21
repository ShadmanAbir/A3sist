using A3sist.Core.Services;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class PerformanceMonitoringServiceTests : IDisposable
    {
        private readonly Mock<ILogger<PerformanceMonitoringService>> _mockLogger;
        private PerformanceMonitoringService? _service;

        public PerformanceMonitoringServiceTests()
        {
            _mockLogger = new Mock<ILogger<PerformanceMonitoringService>>();
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Act
            _service = new PerformanceMonitoringService(_mockLogger.Object);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PerformanceMonitoringService(null!));
        }

        [Fact]
        public async Task RecordMetricAsync_WithValidMetric_ShouldRecordSuccessfully()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metric = new PerformanceMetric
            {
                Name = "test.metric",
                Type = MetricType.Counter,
                Value = 1.0,
                Unit = "count"
            };

            // Act
            await _service.RecordMetricAsync(metric);

            // Assert
            var metrics = await _service.GetMetricsAsync("test.metric");
            Assert.Single(metrics);
            Assert.Equal("test.metric", metrics.First().Name);
        }

        [Fact]
        public async Task RecordMetricAsync_WithNullMetric_ShouldThrowArgumentNullException()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RecordMetricAsync(null!));
        }

        [Fact]
        public async Task RecordCounterAsync_ShouldRecordCounterMetric()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.counter";
            var value = 5.0;
            var tags = new Dictionary<string, string> { ["component"] = "test" };

            // Act
            await _service.RecordCounterAsync(metricName, value, tags);

            // Assert
            var metrics = await _service.GetMetricsAsync(metricName);
            var metric = metrics.First();
            Assert.Equal(metricName, metric.Name);
            Assert.Equal(MetricType.Counter, metric.Type);
            Assert.Equal(value, metric.Value);
            Assert.Equal("count", metric.Unit);
            Assert.Equal("test", metric.Tags["component"]);
        }

        [Fact]
        public async Task RecordGaugeAsync_ShouldRecordGaugeMetric()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.gauge";
            var value = 42.5;

            // Act
            await _service.RecordGaugeAsync(metricName, value);

            // Assert
            var metrics = await _service.GetMetricsAsync(metricName);
            var metric = metrics.First();
            Assert.Equal(metricName, metric.Name);
            Assert.Equal(MetricType.Gauge, metric.Type);
            Assert.Equal(value, metric.Value);
        }

        [Fact]
        public async Task RecordHistogramAsync_ShouldRecordHistogramMetric()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.histogram";
            var value = 123.45;

            // Act
            await _service.RecordHistogramAsync(metricName, value);

            // Assert
            var metrics = await _service.GetMetricsAsync(metricName);
            var metric = metrics.First();
            Assert.Equal(metricName, metric.Name);
            Assert.Equal(MetricType.Histogram, metric.Type);
            Assert.Equal(value, metric.Value);
        }

        [Fact]
        public async Task RecordTimingAsync_ShouldRecordTimerMetric()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.timing";
            var duration = TimeSpan.FromMilliseconds(500);

            // Act
            await _service.RecordTimingAsync(metricName, duration);

            // Assert
            var metrics = await _service.GetMetricsAsync(metricName);
            var metric = metrics.First();
            Assert.Equal(metricName, metric.Name);
            Assert.Equal(MetricType.Timer, metric.Type);
            Assert.Equal(duration.TotalMilliseconds, metric.Value);
            Assert.Equal("ms", metric.Unit);
        }

        [Fact]
        public async Task StartTimer_ShouldRecordTimingWhenDisposed()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.timer";

            // Act
            using (var timer = _service.StartTimer(metricName))
            {
                await Task.Delay(10); // Small delay to ensure measurable time
            }

            // Give some time for the async recording to complete
            await Task.Delay(100);

            // Assert
            var metrics = await _service.GetMetricsAsync(metricName);
            Assert.Single(metrics);
            var metric = metrics.First();
            Assert.Equal(metricName, metric.Name);
            Assert.Equal(MetricType.Timer, metric.Type);
            Assert.True(metric.Value > 0);
        }

        [Fact]
        public async Task GetMetricsAsync_WithTimeFilter_ShouldReturnFilteredMetrics()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.filtered";
            var startTime = DateTime.UtcNow;

            // Record metrics before and after start time
            await _service.RecordCounterAsync(metricName, 1);
            await Task.Delay(10);
            var filterStartTime = DateTime.UtcNow;
            await Task.Delay(10);
            await _service.RecordCounterAsync(metricName, 2);

            // Act
            var allMetrics = await _service.GetMetricsAsync(metricName);
            var filteredMetrics = await _service.GetMetricsAsync(metricName, filterStartTime);

            // Assert
            Assert.Equal(2, allMetrics.Count());
            Assert.Single(filteredMetrics);
            Assert.Equal(2, filteredMetrics.First().Value);
        }

        [Fact]
        public async Task GetMetricsAsync_WithTagFilter_ShouldReturnFilteredMetrics()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.tagged";
            var tags1 = new Dictionary<string, string> { ["env"] = "test" };
            var tags2 = new Dictionary<string, string> { ["env"] = "prod" };

            await _service.RecordCounterAsync(metricName, 1, tags1);
            await _service.RecordCounterAsync(metricName, 2, tags2);

            // Act
            var allMetrics = await _service.GetMetricsAsync(metricName);
            var filteredMetrics = await _service.GetMetricsAsync(metricName, tags: tags1);

            // Assert
            Assert.Equal(2, allMetrics.Count());
            Assert.Single(filteredMetrics);
            Assert.Equal(1, filteredMetrics.First().Value);
        }

        [Fact]
        public async Task GetStatisticsAsync_WithMultipleValues_ShouldCalculateCorrectStatistics()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "test.stats";
            var values = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

            foreach (var value in values)
            {
                await _service.RecordGaugeAsync(metricName, value);
            }

            // Act
            var stats = await _service.GetStatisticsAsync(metricName);

            // Assert
            Assert.Equal(metricName, stats.MetricName);
            Assert.Equal(5, stats.Count);
            Assert.Equal(1.0, stats.Min);
            Assert.Equal(5.0, stats.Max);
            Assert.Equal(3.0, stats.Average);
            Assert.Equal(3.0, stats.Median);
            Assert.Equal(15.0, stats.Sum);
        }

        [Fact]
        public async Task GetStatisticsAsync_WithNoMetrics_ShouldReturnEmptyStatistics()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metricName = "nonexistent.metric";

            // Act
            var stats = await _service.GetStatisticsAsync(metricName);

            // Assert
            Assert.Equal(metricName, stats.MetricName);
            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Sum);
        }

        [Fact]
        public async Task GetSystemHealthAsync_ShouldReturnHealthMetrics()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);

            // Act
            var health = await _service.GetSystemHealthAsync();

            // Assert
            Assert.NotNull(health);
            Assert.True(health.Cpu.CoreCount > 0);
            Assert.True(health.Application.Uptime >= TimeSpan.Zero);
            Assert.True(health.Application.ThreadCount > 0);
        }

        [Fact]
        public async Task CleanupMetricsAsync_ShouldNotThrow()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            await _service.RecordCounterAsync("test.cleanup", 1);

            // Act & Assert
            await _service.CleanupMetricsAsync(); // Should not throw
        }

        [Fact]
        public async Task RecordMetricAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            var metric = new PerformanceMetric { Name = "test", Value = 1 };
            _service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.RecordMetricAsync(metric));
        }

        [Fact]
        public async Task GetMetricsAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _service = new PerformanceMonitoringService(_mockLogger.Object);
            _service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.GetMetricsAsync());
        }

        public void Dispose()
        {
            _service?.Dispose();
        }
    }
}