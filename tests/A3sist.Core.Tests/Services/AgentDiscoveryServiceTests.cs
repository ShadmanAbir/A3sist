using A3sist.Core.Services;
using A3sist.Shared.Attributes;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class AgentDiscoveryServiceTests
    {
        private readonly Mock<ILogger<AgentDiscoveryService>> _mockLogger;
        private readonly AgentDiscoveryService _discoveryService;

        public AgentDiscoveryServiceTests()
        {
            _mockLogger = new Mock<ILogger<AgentDiscoveryService>>();
            _discoveryService = new AgentDiscoveryService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesSuccessfully()
        {
            // Assert
            Assert.NotNull(_discoveryService);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AgentDiscoveryService(null));
        }

        [Fact]
        public async Task DiscoverAgentsAsync_WithValidAssembly_FindsAgents()
        {
            // Arrange
            var assembly = Assembly.GetExecutingAssembly();

            // Act
            var agentTypes = await _discoveryService.DiscoverAgentsAsync(assembly);

            // Assert
            Assert.Contains(typeof(TestDiscoveryAgent), agentTypes);
        }

        [Fact]
        public async Task DiscoverAgentsAsync_WithNullAssembly_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _discoveryService.DiscoverAgentsAsync(null));
        }

        [Fact]
        public async Task DiscoverAllAgentsAsync_FindsAgentsInCurrentDomain()
        {
            // Act
            var agentTypes = await _discoveryService.DiscoverAllAgentsAsync();

            // Assert
            Assert.NotEmpty(agentTypes);
            Assert.Contains(typeof(TestDiscoveryAgent), agentTypes);
        }

        [Fact]
        public async Task GetAgentMetadataAsync_WithValidType_ReturnsMetadata()
        {
            // Act
            var metadata = await _discoveryService.GetAgentMetadataAsync(typeof(TestDiscoveryAgent));

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("TestDiscoveryAgent", metadata.Name);
            Assert.Equal(AgentType.Unknown, metadata.Type);
            Assert.Contains("discovery", metadata.Keywords);
            Assert.Contains(".discovery", metadata.SupportedFileExtensions);
        }

        [Fact]
        public async Task GetAgentMetadataAsync_WithNullType_ReturnsNull()
        {
            // Act
            var metadata = await _discoveryService.GetAgentMetadataAsync(null);

            // Assert
            Assert.Null(metadata);
        }

        [Fact]
        public async Task GetAllAgentMetadataAsync_ReturnsMetadataForAllAgents()
        {
            // Act
            var metadataList = await _discoveryService.GetAllAgentMetadataAsync();

            // Assert
            Assert.NotEmpty(metadataList);
            Assert.Contains(metadataList, m => m.Name == "TestDiscoveryAgent");
        }

        [Fact]
        public async Task ValidateAgentTypeAsync_WithValidType_ReturnsValid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(typeof(TestDiscoveryAgent));

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateAgentTypeAsync_WithNullType_ReturnsInvalid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(null);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Agent type cannot be null", result.Errors);
        }

        [Fact]
        public async Task ValidateAgentTypeAsync_WithNonAgentType_ReturnsInvalid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(typeof(string));

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("does not implement IAgent interface"));
        }

        [Fact]
        public async Task ValidateAgentTypeAsync_WithAbstractType_ReturnsInvalid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(typeof(AbstractDiscoveryAgent));

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("is abstract and cannot be instantiated"));
        }

        [Fact]
        public async Task ValidateAgentTypeAsync_WithInterfaceType_ReturnsInvalid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(typeof(IAgent));

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("is an interface and cannot be instantiated"));
        }

        [Fact]
        public async Task AutoRegisterAgentsAsync_WithValidFactory_RegistersAgents()
        {
            // Arrange
            var mockFactory = new Mock<IAgentFactory>();
            mockFactory.Setup(x => x.RegisterAgentTypeAsync(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _discoveryService.AutoRegisterAgentsAsync(mockFactory.Object, Assembly.GetExecutingAssembly());

            // Assert
            mockFactory.Verify(x => x.RegisterAgentTypeAsync(typeof(TestDiscoveryAgent), null), Times.Once);
        }

        [Fact]
        public async Task AutoRegisterAgentsAsync_WithNullFactory_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _discoveryService.AutoRegisterAgentsAsync(null));
        }

        [Fact]
        public async Task GetAgentMetadataAsync_CachesResults()
        {
            // Act
            var metadata1 = await _discoveryService.GetAgentMetadataAsync(typeof(TestDiscoveryAgent));
            var metadata2 = await _discoveryService.GetAgentMetadataAsync(typeof(TestDiscoveryAgent));

            // Assert
            Assert.Same(metadata1, metadata2); // Should be the same instance due to caching
        }
    }

    /// <summary>
    /// Test agent for discovery testing
    /// </summary>
    [AgentCapability("DiscoveryCapability", 
        Description = "Test capability for discovery testing",
        AgentType = AgentType.Unknown,
        Keywords = "discovery,test",
        FileExtensions = ".discovery,.test",
        Priority = 5)]
    public class TestDiscoveryAgent : IAgent
    {
        public string Name => "TestDiscoveryAgent";
        public AgentType Type => AgentType.Unknown;

        public TestDiscoveryAgent(ILogger<TestDiscoveryAgent> logger, IAgentConfiguration configuration)
        {
            // Constructor for DI
        }

        public Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AgentResult.CreateSuccess("Discovery test success"));
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
    /// Abstract test agent for validation testing
    /// </summary>
    public abstract class AbstractDiscoveryAgent : IAgent
    {
        public abstract string Name { get; }
        public abstract AgentType Type { get; }

        public abstract Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default);
        public abstract Task<bool> CanHandleAsync(AgentRequest request);
        public abstract Task InitializeAsync();
        public abstract Task ShutdownAsync();
    }
}