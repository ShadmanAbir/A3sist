using A3sist.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace A3sist.Core.Tests.Logging
{
    public class LoggingScopeTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public LoggingScopeTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void ForAgent_ShouldCreateScopeWithCorrectProperties()
        {
            // Arrange
            var agentName = "TestAgent";
            var requestId = "req-123";
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.ForAgent(_mockLogger.Object, agentName, requestId);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d.ContainsKey("AgentName") && 
                d.ContainsKey("RequestId") && 
                d.ContainsKey("OperationId") &&
                d["AgentName"].Equals(agentName) &&
                d["RequestId"].Equals(requestId))), Times.Once);
        }

        [Fact]
        public void ForOrchestrator_ShouldCreateScopeWithCorrectProperties()
        {
            // Arrange
            var requestId = "req-123";
            var operation = "ProcessRequest";
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.ForOrchestrator(_mockLogger.Object, requestId, operation);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d.ContainsKey("Component") && 
                d.ContainsKey("RequestId") && 
                d.ContainsKey("Operation") &&
                d.ContainsKey("OperationId") &&
                d["Component"].Equals("Orchestrator") &&
                d["RequestId"].Equals(requestId) &&
                d["Operation"].Equals(operation))), Times.Once);
        }

        [Fact]
        public void ForUser_ShouldCreateScopeWithCorrectProperties()
        {
            // Arrange
            var userId = "user123";
            var sessionId = "session456";
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.ForUser(_mockLogger.Object, userId, sessionId);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d.ContainsKey("UserId") && 
                d.ContainsKey("SessionId") && 
                d.ContainsKey("OperationId") &&
                d["UserId"].Equals(userId) &&
                d["SessionId"].Equals(sessionId))), Times.Once);
        }

        [Fact]
        public void ForUser_WithNullValues_ShouldUseDefaults()
        {
            // Arrange
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.ForUser(_mockLogger.Object);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d["UserId"].Equals("Anonymous") &&
                d["SessionId"].Equals("Unknown"))), Times.Once);
        }

        [Fact]
        public void ForPerformance_ShouldCreateScopeWithCorrectProperties()
        {
            // Arrange
            var operationName = "DatabaseQuery";
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.ForPerformance(_mockLogger.Object, operationName);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d.ContainsKey("PerformanceOperation") && 
                d.ContainsKey("StartTime") && 
                d.ContainsKey("OperationId") &&
                d["PerformanceOperation"].Equals(operationName))), Times.Once);
        }

        [Fact]
        public void ForExternalService_ShouldCreateScopeWithCorrectProperties()
        {
            // Arrange
            var serviceName = "OpenAI";
            var operation = "ChatCompletion";
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.ForExternalService(_mockLogger.Object, serviceName, operation);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d.ContainsKey("ExternalService") && 
                d.ContainsKey("ServiceOperation") && 
                d.ContainsKey("CallId") &&
                d["ExternalService"].Equals(serviceName) &&
                d["ServiceOperation"].Equals(operation))), Times.Once);
        }

        [Fact]
        public void WithProperties_ShouldCreateScopeWithProvidedProperties()
        {
            // Arrange
            var properties = new Dictionary<string, object>
            {
                ["CustomProperty1"] = "Value1",
                ["CustomProperty2"] = 42
            };
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(properties))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.WithProperties(_mockLogger.Object, properties);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(properties), Times.Once);
        }

        [Fact]
        public void WithProperty_ShouldCreateScopeWithSingleProperty()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()))
                      .Returns(mockScope.Object);

            // Act
            var scope = LoggingScope.WithProperty(_mockLogger.Object, key, value);

            // Assert
            Assert.NotNull(scope);
            _mockLogger.Verify(x => x.BeginScope(It.Is<Dictionary<string, object>>(d => 
                d.ContainsKey(key) && d[key].Equals(value))), Times.Once);
        }
    }

    public class TimedOperationTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public TimedOperationTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void Constructor_ShouldLogStartMessage()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            using var timedOperation = new TimedOperation(_mockLogger.Object, operationName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting operation") && v.ToString()!.Contains(operationName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_ShouldLogCompletionMessage()
        {
            // Arrange
            var operationName = "TestOperation";
            var timedOperation = new TimedOperation(_mockLogger.Object, operationName);

            // Act
            timedOperation.Dispose();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed operation") && v.ToString()!.Contains(operationName)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldOnlyLogOnce()
        {
            // Arrange
            var operationName = "TestOperation";
            var timedOperation = new TimedOperation(_mockLogger.Object, operationName);

            // Act
            timedOperation.Dispose();
            timedOperation.Dispose(); // Second call

            // Assert - Should only log completion once
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed operation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TimedOperation(null!, "TestOperation"));
        }

        [Fact]
        public void Constructor_WithNullOperationName_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TimedOperation(_mockLogger.Object, null!));
        }
    }

    public class TimedOperationExtensionsTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public TimedOperationExtensionsTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void TimeOperation_ShouldReturnTimedOperation()
        {
            // Arrange
            var operationName = "TestOperation";

            // Act
            using var timedOperation = _mockLogger.Object.TimeOperation(operationName);

            // Assert
            Assert.NotNull(timedOperation);
            Assert.IsType<TimedOperation>(timedOperation);
        }

        [Fact]
        public void TimeOperationWithScope_ShouldReturnTimedOperationWithScope()
        {
            // Arrange
            var operationName = "TestOperation";
            var scopeProperties = new Dictionary<string, object> { ["TestProp"] = "TestValue" };
            var mockScope = new Mock<IDisposable>();
            
            _mockLogger.Setup(x => x.BeginScope(scopeProperties))
                      .Returns(mockScope.Object);

            // Act
            using var timedOperation = _mockLogger.Object.TimeOperationWithScope(operationName, scopeProperties);

            // Assert
            Assert.NotNull(timedOperation);
            Assert.IsType<TimedOperation>(timedOperation);
            _mockLogger.Verify(x => x.BeginScope(scopeProperties), Times.Once);
        }
    }
}