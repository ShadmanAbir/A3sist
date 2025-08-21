using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using A3sist.Shared.Interfaces;

namespace A3sist.Core.Tests.Extensions;

/// <summary>
/// Tests for basic service configuration
/// </summary>
public class BasicServiceTests
{
    private readonly IConfiguration _configuration;

    public BasicServiceTests()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["A3sist:Agents:Orchestrator:Enabled"] = "true",
                ["A3sist:Agents:Orchestrator:MaxConcurrentTasks"] = "5",
                ["A3sist:LLM:Provider"] = "OpenAI",
                ["A3sist:LLM:Model"] = "gpt-4",
                ["Serilog:MinimumLevel:Default"] = "Information"
            });
        
        _configuration = configBuilder.Build();
    }

    [Fact]
    public void Configuration_ShouldLoadCorrectly()
    {
        // Act & Assert
        _configuration["A3sist:Agents:Orchestrator:Enabled"].Should().Be("true");
        _configuration["A3sist:LLM:Provider"].Should().Be("OpenAI");
    }

    [Fact]
    public void ServiceCollection_ShouldRegisterBasicServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton(_configuration);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IConfiguration>().Should().NotBeNull();
        serviceProvider.GetService<ILoggerFactory>().Should().NotBeNull();
    }
}