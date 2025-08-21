using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;

namespace A3sist.TestUtilities;

/// <summary>
/// Factory for creating commonly used mocks in tests
/// </summary>
public static class MockFactory
{
    /// <summary>
    /// Creates a mock ILogger with basic setup
    /// </summary>
    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        var mock = new Mock<ILogger<T>>();
        
        // Setup IsEnabled to return true for all log levels by default
        mock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);
            
        return mock;
    }

    /// <summary>
    /// Creates a mock IConfiguration with test values
    /// </summary>
    public static Mock<IConfiguration> CreateConfiguration()
    {
        var mock = new Mock<IConfiguration>();
        var testConfig = new TestConfiguration();
        
        mock.Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => testConfig.GetValue(key));
            
        return mock;
    }

    /// <summary>
    /// Creates a mock IAgent with basic setup
    /// </summary>
    public static Mock<IAgent> CreateAgent(string name = "TestAgent", AgentType type = AgentType.Analyzer)
    {
        var mock = new Mock<IAgent>();
        
        mock.Setup(x => x.Name).Returns(name);
        mock.Setup(x => x.Type).Returns(type);
        mock.Setup(x => x.CanHandleAsync(It.IsAny<AgentRequest>()))
            .ReturnsAsync(true);
        mock.Setup(x => x.InitializeAsync())
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.ShutdownAsync())
            .Returns(Task.CompletedTask);
            
        return mock;
    }

    /// <summary>
    /// Creates a mock IAgentManager with basic setup
    /// </summary>
    public static Mock<IAgentManager> CreateAgentManager()
    {
        var mock = new Mock<IAgentManager>();
        
        mock.Setup(x => x.RegisterAgentAsync(It.IsAny<IAgent>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.UnregisterAgentAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
            
        return mock;
    }

    /// <summary>
    /// Creates a mock IOrchestrator with basic setup
    /// </summary>
    public static Mock<IOrchestrator> CreateOrchestrator()
    {
        var mock = new Mock<IOrchestrator>();
        
        mock.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult { Success = true, Message = "Test result" });
        mock.Setup(x => x.RegisterAgentAsync(It.IsAny<IAgent>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.UnregisterAgentAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
            
        return mock;
    }

    /// <summary>
    /// Creates a mock ITaskQueueService with basic setup
    /// </summary>
    public static Mock<ITaskQueueService> CreateTaskQueueService()
    {
        var mock = new Mock<ITaskQueueService>();
        
        mock.Setup(x => x.EnqueueAsync(It.IsAny<AgentRequest>(), It.IsAny<TaskPriority>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentRequest { Id = Guid.NewGuid(), Prompt = "Test request" });
            
        return mock;
    }

    /// <summary>
    /// Creates a mock ILoggingService with basic setup
    /// </summary>
    public static Mock<ILoggingService> CreateLoggingService()
    {
        var mock = new Mock<ILoggingService>();
        
        // Setup basic logging functionality - actual methods depend on interface definition
            
        return mock;
    }

    /// <summary>
    /// Creates a mock IPerformanceMonitoringService with basic setup
    /// </summary>
    public static Mock<IPerformanceMonitoringService> CreatePerformanceMonitoringService()
    {
        var mock = new Mock<IPerformanceMonitoringService>();
        
        mock.Setup(x => x.RecordMetricAsync(It.IsAny<PerformanceMetric>()))
            .Returns(Task.CompletedTask);
        mock.Setup(x => x.GetStatisticsAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(new PerformanceStatistics());
            
        return mock;
    }

    /// <summary>
    /// Creates a mock IErrorReportingService with basic setup
    /// </summary>
    public static Mock<IErrorReportingService> CreateErrorReportingService()
    {
        var mock = new Mock<IErrorReportingService>();
        
        mock.Setup(x => x.ReportErrorAsync(It.IsAny<ErrorReport>()))
            .Returns(Task.CompletedTask);
        // Setup basic error reporting functionality
            
        return mock;
    }
}