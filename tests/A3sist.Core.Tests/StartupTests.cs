using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;

namespace A3sist.Core.Tests;

/// <summary>
/// Tests for the shared components and basic functionality
/// </summary>
public class SharedComponentsTests
{
    [Fact]
    public void AgentStatus_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var agentStatus = new AgentStatus
        {
            AgentId = "test-agent",
            Status = "Running",
            LastUpdated = DateTime.UtcNow
        };

        // Assert
        agentStatus.AgentId.Should().Be("test-agent");
        agentStatus.Status.Should().Be("Running");
        agentStatus.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ServiceCollection_ShouldAllowBasicRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestKey"] = "TestValue"
            })
            .Build();

        // Act
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var config = serviceProvider.GetService<IConfiguration>();
        config.Should().NotBeNull();
        config!["TestKey"].Should().Be("TestValue");
        
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }
}