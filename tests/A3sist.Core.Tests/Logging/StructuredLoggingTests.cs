using A3sist.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace A3sist.Core.Tests.Logging
{
    public class StructuredLoggingTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public StructuredLoggingTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void LogAgentExecutionStart_ShouldLogWithCorrectParameters()
        {
            // Arrange
            var agentName = "TestAgent";
            var requestId = "req-123";
            var operation = "TestOperation";

            // Act
            _mockLogger.Object.LogAgentExecutionStart(agentName, requestId, operation);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(agentName) && v.ToString()!.Contains(requestId) && v.ToString()!.Contains(operation)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogAgentExecutionComplete_WithSuccess_ShouldLogInformation()
        {
            // Arrange
            var agentName = "TestAgent";
            var requestId = "req-123";
            var operation = "TestOperation";
            var duration = TimeSpan.FromMilliseconds(500);

            // Act
            _mockLogger.Object.LogAgentExecutionComplete(agentName, requestId, operation, duration, true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogAgentExecutionComplete_WithFailure_ShouldLogWarning()
        {
            // Arrange
            var agentName = "TestAgent";
            var requestId = "req-123";
            var operation = "TestOperation";
            var duration = TimeSpan.FromMilliseconds(500);

            // Act
            _mockLogger.Object.LogAgentExecutionComplete(agentName, requestId, operation, duration, false);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogAgentExecutionError_ShouldLogError()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error");
            var agentName = "TestAgent";
            var requestId = "req-123";
            var operation = "TestOperation";
            var duration = TimeSpan.FromMilliseconds(500);

            // Act
            _mockLogger.Object.LogAgentExecutionError(exception, agentName, requestId, operation, duration);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("error")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogOrchestratorRequest_ShouldLogWithCorrectParameters()
        {
            // Arrange
            var requestId = "req-123";
            var requestType = "CodeAnalysis";
            var agentCount = 5;
            var preferredAgent = "CSharpAgent";

            // Act
            _mockLogger.Object.LogOrchestratorRequest(requestId, requestType, agentCount, preferredAgent);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Orchestrator") && v.ToString()!.Contains(requestId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogConfigurationChange_ShouldLogWithCorrectParameters()
        {
            // Arrange
            var configurationSection = "Logging";
            var changedBy = "TestUser";
            var changes = new Dictionary<string, object> { ["Level"] = "Debug" };

            // Act
            _mockLogger.Object.LogConfigurationChange(configurationSection, changedBy, changes);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuration changed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogPerformanceMetric_ShouldLogWithCorrectParameters()
        {
            // Arrange
            var metricName = "ResponseTime";
            var value = 123.45;
            var unit = "ms";
            var tags = new Dictionary<string, object> { ["Agent"] = "TestAgent" };

            // Act
            _mockLogger.Object.LogPerformanceMetric(metricName, value, unit, tags);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Performance metric")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogHealthCheck_WithHealthyStatus_ShouldLogInformation()
        {
            // Arrange
            var componentName = "TestComponent";
            var responseTime = TimeSpan.FromMilliseconds(100);

            // Act
            _mockLogger.Object.LogHealthCheck(componentName, true, responseTime, "All good");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health check") && v.ToString()!.Contains("Healthy")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogHealthCheck_WithUnhealthyStatus_ShouldLogWarning()
        {
            // Arrange
            var componentName = "TestComponent";
            var responseTime = TimeSpan.FromMilliseconds(5000);

            // Act
            _mockLogger.Object.LogHealthCheck(componentName, false, responseTime, "Service unavailable");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health check") && v.ToString()!.Contains("Unhealthy")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogResourceUsage_ShouldLogWithCorrectParameters()
        {
            // Arrange
            var resourceType = "Memory";
            var currentUsage = 512.0;
            var maxUsage = 1024.0;
            var unit = "MB";

            // Act
            _mockLogger.Object.LogResourceUsage(resourceType, currentUsage, maxUsage, unit);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Resource usage") && v.ToString()!.Contains("50.0%")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogUserAction_ShouldLogWithCorrectParameters()
        {
            // Arrange
            var action = "FileOpen";
            var userId = "user123";
            var context = new Dictionary<string, object> { ["FileName"] = "test.cs" };

            // Act
            _mockLogger.Object.LogUserAction(action, userId, context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User action") && v.ToString()!.Contains(action)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogSecurityEvent_WithSuccess_ShouldLogInformation()
        {
            // Arrange
            var eventType = "Login";
            var description = "User logged in successfully";
            var userId = "user123";
            var ipAddress = "192.168.1.1";

            // Act
            _mockLogger.Object.LogSecurityEvent(eventType, description, userId, ipAddress, true);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogSecurityEvent_WithFailure_ShouldLogWarning()
        {
            // Arrange
            var eventType = "Login";
            var description = "Failed login attempt";
            var userId = "user123";
            var ipAddress = "192.168.1.1";

            // Act
            _mockLogger.Object.LogSecurityEvent(eventType, description, userId, ipAddress, false);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security event")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogExternalServiceCall_WithSuccess_ShouldLogInformation()
        {
            // Arrange
            var serviceName = "OpenAI";
            var operation = "ChatCompletion";
            var duration = TimeSpan.FromMilliseconds(1500);
            var statusCode = 200;

            // Act
            _mockLogger.Object.LogExternalServiceCall(serviceName, operation, duration, true, statusCode);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("External service call") && v.ToString()!.Contains("completed successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogExternalServiceCall_WithFailure_ShouldLogWarning()
        {
            // Arrange
            var serviceName = "OpenAI";
            var operation = "ChatCompletion";
            var duration = TimeSpan.FromMilliseconds(5000);
            var statusCode = 500;
            var errorMessage = "Internal server error";

            // Act
            _mockLogger.Object.LogExternalServiceCall(serviceName, operation, duration, false, statusCode, errorMessage);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("External service call") && v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}