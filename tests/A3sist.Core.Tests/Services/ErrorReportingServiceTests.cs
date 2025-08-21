using A3sist.Core.Services;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class ErrorReportingServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ErrorReportingService>> _mockLogger;
        private ErrorReportingService? _service;
        private readonly string _testExportPath;

        public ErrorReportingServiceTests()
        {
            _mockLogger = new Mock<ILogger<ErrorReportingService>>();
            _testExportPath = Path.Combine(Path.GetTempPath(), "A3sistTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testExportPath);
        }

        [Fact]
        public void Constructor_ShouldInitializeSuccessfully()
        {
            // Act
            _service = new ErrorReportingService(_mockLogger.Object);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ErrorReportingService(null!));
        }

        [Fact]
        public async Task ReportErrorAsync_WithValidErrorReport_ShouldReportSuccessfully()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            var errorReport = new ErrorReport
            {
                Message = "Test error",
                Severity = ErrorSeverity.Error,
                Category = ErrorCategory.Application
            };

            // Act
            await _service.ReportErrorAsync(errorReport);

            // Assert
            var errors = await _service.GetErrorReportsAsync();
            Assert.Single(errors);
            Assert.Equal("Test error", errors.First().Message);
        }

        [Fact]
        public async Task ReportErrorAsync_WithNullErrorReport_ShouldThrowArgumentNullException()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ReportErrorAsync(null!));
        }

        [Fact]
        public async Task ReportExceptionAsync_WithException_ShouldCreateErrorReport()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            var exception = new InvalidOperationException("Test exception");
            var context = new Dictionary<string, object> { ["TestKey"] = "TestValue" };

            // Act
            await _service.ReportExceptionAsync(exception, context, ErrorSeverity.Critical, "TestComponent");

            // Assert
            var errors = await _service.GetErrorReportsAsync();
            var error = errors.First();
            Assert.Equal("Test exception", error.Message);
            Assert.Equal(ErrorSeverity.Critical, error.Severity);
            Assert.Equal("TestComponent", error.Component);
            Assert.Equal("TestValue", error.Context["TestKey"]);
            Assert.NotNull(error.Exception);
            Assert.Equal(typeof(InvalidOperationException).FullName, error.Exception.Type);
        }

        [Fact]
        public async Task ReportErrorAsync_WithStringMessage_ShouldCreateErrorReport()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            var message = "Custom error message";
            var context = new Dictionary<string, object> { ["UserId"] = "123" };

            // Act
            await _service.ReportErrorAsync(message, ErrorCategory.Validation, ErrorSeverity.Warning, context, "ValidationComponent");

            // Assert
            var errors = await _service.GetErrorReportsAsync();
            var error = errors.First();
            Assert.Equal(message, error.Message);
            Assert.Equal(ErrorCategory.Validation, error.Category);
            Assert.Equal(ErrorSeverity.Warning, error.Severity);
            Assert.Equal("ValidationComponent", error.Component);
            Assert.Equal("123", error.Context["UserId"]);
        }

        [Fact]
        public async Task GetErrorReportsAsync_WithTimeFilter_ShouldReturnFilteredErrors()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            var startTime = DateTime.UtcNow;

            // Report errors before and after start time
            await _service.ReportErrorAsync("Old error");
            await Task.Delay(10);
            var filterStartTime = DateTime.UtcNow;
            await Task.Delay(10);
            await _service.ReportErrorAsync("New error");

            // Act
            var allErrors = await _service.GetErrorReportsAsync();
            var filteredErrors = await _service.GetErrorReportsAsync(filterStartTime);

            // Assert
            Assert.Equal(2, allErrors.Count());
            Assert.Single(filteredErrors);
            Assert.Equal("New error", filteredErrors.First().Message);
        }

        [Fact]
        public async Task GetErrorReportsAsync_WithSeverityFilter_ShouldReturnFilteredErrors()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            await _service.ReportErrorAsync("Warning error", ErrorCategory.Application, ErrorSeverity.Warning);
            await _service.ReportErrorAsync("Critical error", ErrorCategory.Application, ErrorSeverity.Critical);

            // Act
            var allErrors = await _service.GetErrorReportsAsync();
            var criticalErrors = await _service.GetErrorReportsAsync(severity: ErrorSeverity.Critical);

            // Assert
            Assert.Equal(2, allErrors.Count());
            Assert.Single(criticalErrors);
            Assert.Equal("Critical error", criticalErrors.First().Message);
        }

        [Fact]
        public async Task GetErrorReportsAsync_WithCategoryFilter_ShouldReturnFilteredErrors()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            await _service.ReportErrorAsync("App error", ErrorCategory.Application);
            await _service.ReportErrorAsync("Network error", ErrorCategory.Network);

            // Act
            var allErrors = await _service.GetErrorReportsAsync();
            var networkErrors = await _service.GetErrorReportsAsync(category: ErrorCategory.Network);

            // Assert
            Assert.Equal(2, allErrors.Count());
            Assert.Single(networkErrors);
            Assert.Equal("Network error", networkErrors.First().Message);
        }

        [Fact]
        public async Task GetErrorReportsAsync_WithComponentFilter_ShouldReturnFilteredErrors()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            await _service.ReportErrorAsync("Component A error", component: "ComponentA");
            await _service.ReportErrorAsync("Component B error", component: "ComponentB");

            // Act
            var allErrors = await _service.GetErrorReportsAsync();
            var componentAErrors = await _service.GetErrorReportsAsync(component: "ComponentA");

            // Assert
            Assert.Equal(2, allErrors.Count());
            Assert.Single(componentAErrors);
            Assert.Equal("Component A error", componentAErrors.First().Message);
        }

        [Fact]
        public async Task GetErrorStatisticsAsync_ShouldReturnCorrectStatistics()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            await _service.ReportErrorAsync("Error 1", ErrorCategory.Application, ErrorSeverity.Error, component: "ComponentA");
            await _service.ReportErrorAsync("Error 2", ErrorCategory.Network, ErrorSeverity.Warning, component: "ComponentB");
            await _service.ReportErrorAsync("Error 3", ErrorCategory.Application, ErrorSeverity.Critical, component: "ComponentA");

            // Act
            var stats = await _service.GetErrorStatisticsAsync();

            // Assert
            Assert.Equal(3, stats.TotalErrors);
            Assert.Equal(2, stats.ErrorsByCategory[ErrorCategory.Application]);
            Assert.Equal(1, stats.ErrorsByCategory[ErrorCategory.Network]);
            Assert.Equal(1, stats.ErrorsBySeverity[ErrorSeverity.Error]);
            Assert.Equal(1, stats.ErrorsBySeverity[ErrorSeverity.Warning]);
            Assert.Equal(1, stats.ErrorsBySeverity[ErrorSeverity.Critical]);
            Assert.Equal(2, stats.ErrorsByComponent["ComponentA"]);
            Assert.Equal(1, stats.ErrorsByComponent["ComponentB"]);
        }

        [Fact]
        public async Task GetFrequentErrorsAsync_ShouldReturnFrequentErrors()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            // Report the same error multiple times
            for (int i = 0; i < 5; i++)
            {
                await _service.ReportErrorAsync("Frequent error");
            }

            // Report a different error once
            await _service.ReportErrorAsync("Rare error");

            // Act
            var frequentErrors = await _service.GetFrequentErrorsAsync();

            // Assert
            Assert.Single(frequentErrors); // Only the frequent error should be returned
            var frequentError = frequentErrors.First();
            Assert.Equal("Frequent error", frequentError.Message);
            Assert.Equal(5, frequentError.Occurrences);
        }

        [Fact]
        public async Task CollectDiagnosticInfoAsync_ShouldReturnDiagnosticInfo()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);

            // Act
            var diagnostics = await _service.CollectDiagnosticInfoAsync();

            // Assert
            Assert.NotNull(diagnostics);
            Assert.True(diagnostics.CollectedAt <= DateTime.UtcNow);
            Assert.NotNull(diagnostics.Application);
            Assert.NotNull(diagnostics.System);
            Assert.NotNull(diagnostics.Performance);
            Assert.NotNull(diagnostics.Configuration);
            Assert.NotNull(diagnostics.Errors);
        }

        [Fact]
        public async Task ExportErrorReportsAsync_JsonFormat_ShouldCreateJsonFile()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            await _service.ReportErrorAsync("Test error for export");
            var filePath = Path.Combine(_testExportPath, "errors.json");

            // Act
            await _service.ExportErrorReportsAsync(filePath, ExportFormat.Json);

            // Assert
            Assert.True(File.Exists(filePath));
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Test error for export", content);
            Assert.Contains("\"Message\":", content); // JSON format check
        }

        [Fact]
        public async Task ExportErrorReportsAsync_CsvFormat_ShouldCreateCsvFile()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            await _service.ReportErrorAsync("Test error for CSV export");
            var filePath = Path.Combine(_testExportPath, "errors.csv");

            // Act
            await _service.ExportErrorReportsAsync(filePath, ExportFormat.Csv);

            // Assert
            Assert.True(File.Exists(filePath));
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Test error for CSV export", content);
            Assert.Contains("Timestamp,Message,Severity,Category,Component", content); // CSV header check
        }

        [Fact]
        public async Task ExportErrorReportsAsync_XmlFormat_ShouldCreateXmlFile()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            await _service.ReportErrorAsync("Test error for XML export");
            var filePath = Path.Combine(_testExportPath, "errors.xml");

            // Act
            await _service.ExportErrorReportsAsync(filePath, ExportFormat.Xml);

            // Assert
            Assert.True(File.Exists(filePath));
            var content = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Test error for XML export", content);
            Assert.Contains("<ErrorReports>", content); // XML format check
        }

        [Fact]
        public async Task CleanupErrorReportsAsync_ShouldNotThrow()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            await _service.ReportErrorAsync("Test error for cleanup");

            // Act & Assert
            await _service.CleanupErrorReportsAsync(); // Should not throw
        }

        [Fact]
        public async Task AnalyzeErrorPatternsAsync_ShouldReturnAnalysis()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            await _service.ReportErrorAsync("Pattern error 1");
            await _service.ReportErrorAsync("Pattern error 2");

            // Act
            var analysis = await _service.AnalyzeErrorPatternsAsync();

            // Assert
            Assert.NotNull(analysis);
            Assert.True(analysis.StartTime <= DateTime.UtcNow);
            Assert.True(analysis.EndTime <= DateTime.UtcNow);
            Assert.NotNull(analysis.ErrorPatterns);
            Assert.NotNull(analysis.KeyInsights);
            Assert.NotNull(analysis.Recommendations);
            Assert.NotNull(analysis.RiskAssessment);
        }

        [Fact]
        public async Task ReportErrorAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            var errorReport = new ErrorReport { Message = "Test error" };
            _service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.ReportErrorAsync(errorReport));
        }

        [Fact]
        public async Task GetErrorReportsAsync_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _service = new ErrorReportingService(_mockLogger.Object);
            _service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.GetErrorReportsAsync());
        }

        public void Dispose()
        {
            _service?.Dispose();
            
            try
            {
                if (Directory.Exists(_testExportPath))
                {
                    Directory.Delete(_testExportPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}