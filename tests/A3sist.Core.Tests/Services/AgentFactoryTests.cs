using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class AgentFactoryTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly AgentFactory _agentFactory;
        private readonly Mock<ILogger<AgentFactory>> _mockLogger;

        public AgentFactoryTests()
        {
            var services = new ServiceCollection();
            _mockLogger = new Mock<ILogger<AgentFactory>>();
            services.AddSingleton(_mockLogger.Object);
            
            // Add mock configuration
            var mockConfig = new Mock<IAgentConfiguration>();
            services.AddSingleton(mockConfig.Object);
            
            _serviceProvider = services.BuildServiceProvider();
            _agentFactory = new AgentFactory(_serviceProvider, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Assert
            Assert.NotNull(_agentFactory);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AgentFactory(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AgentFactory(_serviceProvider, null));
        }

        [Fact]
        public async Task RegisterAgentTypeAsync_WithValidType_RegistersSuccessfully()
        {
            // Arrange
            var agentType = typeof(TestFactoryAgent);

            // Act
            await _agentFactory.RegisterAgentTypeAsync(agentType);

            // Assert
            var isRegistered = await _agentFactory.IsAgentRegisteredAsync("TestFactoryAgent");
            Assert.True(isRegistered);
        }

        [Fact]
        public async Task RegisterAgentTypeAsync_WithNullType_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _agentFactory.RegisterAgentTypeAsync(null));
        }

        [Fact]
        public async Task RegisterAgentTypeAsync_WithNonAgentType_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _agentFactory.RegisterAgentTypeAsync(typeof(string)));
        }

        [Fact]
        public async Task RegisterAgentTypeAsync_WithAbstractType_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _agentFactory.RegisterAgentTypeAsync(typeof(AbstractTestAgent)));
        }

        [Fact]
        public async Task CreateAgentAsync_ByName_ReturnsCorrectAgent()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent));

            // Act
            var agent = await _agentFactory.CreateAgentAsync("TestFactoryAgent");

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("TestFactoryAgent", agent.Name);
        }

        [Fact]
        public async Task CreateAgentAsync_ByType_ReturnsCorrectAgent()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent));

            // Act
            var agent = await _agentFactory.CreateAgentAsync(AgentType.Unknown);

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("TestFactoryAgent", agent.Name);
        }

        [Fact]
        public async Task CreateAgentAsync_WithUnregisteredName_ReturnsNull()
        {
            // Act
            var agent = await _agentFactory.CreateAgentAsync("UnregisteredAgent");

            // Assert
            Assert.Null(agent);
        }

        [Fact]
        public async Task CreateAgentAsync_WithUnregisteredType_ReturnsNull()
        {
            // Act
            var agent = await _agentFactory.CreateAgentAsync(AgentType.Fixer);

            // Assert
            Assert.Null(agent);
        }

        [Fact]
        public async Task CreateAgentByTypeAsync_WithValidTypeName_ReturnsAgent()
        {
            // Arrange
            var typeName = typeof(TestFactoryAgent).AssemblyQualifiedName;

            // Act
            var agent = await _agentFactory.CreateAgentByTypeAsync(typeName);

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("TestFactoryAgent", agent.Name);
        }

        [Fact]
        public async Task CreateAgentByTypeAsync_WithInvalidTypeName_ReturnsNull()
        {
            // Act
            var agent = await _agentFactory.CreateAgentByTypeAsync("InvalidTypeName");

            // Assert
            Assert.Null(agent);
        }

        [Fact]
        public async Task GetAvailableAgentTypesAsync_ReturnsRegisteredTypes()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent));

            // Act
            var types = await _agentFactory.GetAvailableAgentTypesAsync();

            // Assert
            Assert.Contains(typeof(TestFactoryAgent), types);
        }

        [Fact]
        public async Task GetRegisteredAgentNamesAsync_ReturnsRegisteredNames()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent));

            // Act
            var names = await _agentFactory.GetRegisteredAgentNamesAsync();

            // Assert
            Assert.Contains("TestFactoryAgent", names);
        }

        [Fact]
        public async Task UnregisterAgentTypeAsync_WithRegisteredAgent_UnregistersSuccessfully()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent));
            Assert.True(await _agentFactory.IsAgentRegisteredAsync("TestFactoryAgent"));

            // Act
            await _agentFactory.UnregisterAgentTypeAsync("TestFactoryAgent");

            // Assert
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("TestFactoryAgent"));
        }

        [Fact]
        public async Task UnregisterAgentTypeAsync_WithUnregisteredAgent_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            await _agentFactory.UnregisterAgentTypeAsync("UnregisteredAgent");
        }

        [Fact]
        public async Task IsAgentRegisteredAsync_WithRegisteredAgent_ReturnsTrue()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent));

            // Act & Assert
            Assert.True(await _agentFactory.IsAgentRegisteredAsync("TestFactoryAgent"));
        }

        [Fact]
        public async Task IsAgentRegisteredAsync_WithUnregisteredAgent_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("UnregisteredAgent"));
        }

        [Fact]
        public async Task RegisterAgentTypeAsync_WithCustomName_RegistersWithCustomName()
        {
            // Arrange
            var customName = "CustomTestAgent";

            // Act
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestFactoryAgent), customName);

            // Assert
            Assert.True(await _agentFactory.IsAgentRegisteredAsync(customName));
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("TestFactoryAgent"));
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Test agent for factory testing
    /// </summary>
    public class TestFactoryAgent : IAgent
    {
        public string Name => "TestFactoryAgent";
        public AgentType Type => AgentType.Unknown;

        public TestFactoryAgent(ILogger<TestFactoryAgent> logger, IAgentConfiguration configuration)
        {
            // Constructor for DI
        }

        public Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AgentResult.CreateSuccess("Test success"));
        }

        public Task<bool> CanHandleAsync(AgentRequest request)
        {
            return Task.FromResult(true);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Abstract test agent for testing validation
    /// </summary>
    public abstract class AbstractTestAgent : IAgent
    {
        public abstract string Name { get; }
        public abstract AgentType Type { get; }

        public abstract Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default);
        public abstract Task<bool> CanHandleAsync(AgentRequest request);
        public abstract Task InitializeAsync();
        public abstract Task ShutdownAsync();
    }
}