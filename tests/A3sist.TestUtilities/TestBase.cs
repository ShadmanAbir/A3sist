using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Shared.Interfaces;

namespace A3sist.TestUtilities;

/// <summary>
/// Base class for all unit tests providing common setup and utilities
/// </summary>
public abstract class TestBase : IDisposable
{
    protected ServiceProvider ServiceProvider { get; private set; }
    protected IServiceCollection Services { get; private set; }
    protected Mock<ILogger> MockLogger { get; private set; }
    protected Mock<IConfiguration> MockConfiguration { get; private set; }
    protected TestConfiguration TestConfig { get; private set; }

    protected TestBase()
    {
        Services = new ServiceCollection();
        MockLogger = new Mock<ILogger>();
        MockConfiguration = new Mock<IConfiguration>();
        TestConfig = new TestConfiguration();
        
        SetupBasicServices();
        ConfigureServices(Services);
        ServiceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Override this method to configure additional services for specific test classes
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes to add specific services
    }

    private void SetupBasicServices()
    {
        Services.AddSingleton(MockLogger.Object);
        Services.AddSingleton(MockConfiguration.Object);
        Services.AddSingleton<TestConfiguration>(TestConfig);
        Services.AddLogging();
    }

    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public virtual void Dispose()
    {
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}