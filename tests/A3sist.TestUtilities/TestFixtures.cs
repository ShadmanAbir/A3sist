using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Shared.Interfaces;


namespace A3sist.TestUtilities;

/// <summary>
/// Test fixtures for common test scenarios
/// </summary>
public class AgentTestFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; private set; }
    public Mock<ILogger> MockLogger { get; private set; }
    public Mock<IAgentManager> MockAgentManager { get; private set; }
    public Mock<IOrchestrator> MockOrchestrator { get; private set; }
    public TestConfiguration TestConfig { get; private set; }

    public AgentTestFixture()
    {
        var services = new ServiceCollection();
        
        MockLogger = new Mock<ILogger>();
        MockAgentManager = MockFactory.CreateAgentManager();
        MockOrchestrator = MockFactory.CreateOrchestrator();
        TestConfig = new TestConfiguration();

        services.AddSingleton(MockLogger.Object);
        services.AddSingleton(MockAgentManager.Object);
        services.AddSingleton(MockOrchestrator.Object);
        services.AddSingleton(TestConfig.Build());
        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Test fixture for orchestration scenarios
/// </summary>
public class OrchestrationTestFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; private set; }
    public Mock<ILogger> MockLogger { get; private set; }
    public Mock<IAgentManager> MockAgentManager { get; private set; }
    public Mock<ITaskQueueService> MockTaskQueue { get; private set; }
    public Mock<ILoggingService> MockLoggingService { get; private set; }
    public TestConfiguration TestConfig { get; private set; }

    public OrchestrationTestFixture()
    {
        var services = new ServiceCollection();
        
        MockLogger = new Mock<ILogger>();
        MockAgentManager = MockFactory.CreateAgentManager();
        MockTaskQueue = MockFactory.CreateTaskQueueService();
        MockLoggingService = MockFactory.CreateLoggingService();
        TestConfig = new TestConfiguration();

        services.AddSingleton(MockLogger.Object);
        services.AddSingleton(MockAgentManager.Object);
        services.AddSingleton(MockTaskQueue.Object);
        services.AddSingleton(MockLoggingService.Object);
        services.AddSingleton(TestConfig.Build());
        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Test fixture for Visual Studio integration scenarios
/// </summary>
public class VSIntegrationTestFixture : IDisposable
{
    public ServiceProvider ServiceProvider { get; private set; }
    public Mock<ILogger> MockLogger { get; private set; }
    public TestConfiguration TestConfig { get; private set; }

    public VSIntegrationTestFixture()
    {
        var services = new ServiceCollection();
        
        MockLogger = new Mock<ILogger>();
        TestConfig = new TestConfiguration();

        services.AddSingleton(MockLogger.Object);
        services.AddSingleton(TestConfig.Build());
        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}

