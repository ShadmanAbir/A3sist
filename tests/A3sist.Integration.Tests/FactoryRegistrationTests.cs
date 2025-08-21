using A3sist.Core.Services;
using A3sist.Integration.Tests.TestAgents;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Integration.Tests
{
    /// <summary>
    /// Tests for agent factory and registration system functionality
    /// </summary>
    public class FactoryRegistrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IAgentFactory _agentFactory;
        private readonly IAgentDiscoveryService _discoveryService;

        public FactoryRegistrationTests()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Add our services
            services.AddSingleton<IAgentFactory, AgentFactory>();
            services.AddSingleton<IAgentDiscoveryService, AgentDiscoveryService>();
            
            // Add mock configuration
            var mockConfig = new Mock<IAgentConfiguration>();
            services.AddSingleton(mockConfig.Object);
            
            _serviceProvider = services.BuildServiceProvider();
            _agentFactory = _serviceProvider.GetRequiredService<IAgentFactory>();
            _discoveryService = _serviceProvider.GetRequiredService<IAgentDiscoveryService>();
        }

        [Fact]
        public async Task AgentFactory_RegisterAndCreateAgent_Success()
        {
            // Arrange
            var agentType = typeof(MinimalTestAgent);

            // Act
            await _agentFactory.RegisterAgentTypeAsync(agentType);
            var agent = await _agentFactory.CreateAgentAsync("MinimalTestAgent");

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("MinimalTestAgent", agent.Name);
            Assert.Equal(AgentType.Unknown, agent.Type);
        }

        [Fact]
        public async Task AgentFactory_CreateAgentByType_Success()
        {
            // Arrange
            var agentType = typeof(MinimalTestAgent);
            await _agentFactory.RegisterAgentTypeAsync(agentType);

            // Act
            var agent = await _agentFactory.CreateAgentAsync(AgentType.Unknown);

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("MinimalTestAgent", agent.Name);
        }

        [Fact]
        public async Task AgentDiscoveryService_DiscoverAgents_FindsTestAgent()
        {
            // Act
            var agentTypes = await _discoveryService.DiscoverAgentsAsync(Assembly.GetExecutingAssembly());

            // Assert
            Assert.Contains(typeof(MinimalTestAgent), agentTypes);
        }

        [Fact]
        public async Task AgentDiscoveryService_GetAgentMetadata_ReturnsCorrectMetadata()
        {
            // Act
            var metadata = await _discoveryService.GetAgentMetadataAsync(typeof(MinimalTestAgent));

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("MinimalTestAgent", metadata.Name);
            Assert.Equal(AgentType.Unknown, metadata.Type);
            Assert.Contains("test", metadata.Keywords);
            Assert.Contains(".test", metadata.SupportedFileExtensions);
        }

        [Fact]
        public async Task AgentDiscoveryService_ValidateAgentType_ReturnsValid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(typeof(MinimalTestAgent));

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task AgentDiscoveryService_AutoRegisterAgents_RegistersSuccessfully()
        {
            // Act
            await _discoveryService.AutoRegisterAgentsAsync(_agentFactory, Assembly.GetExecutingAssembly());

            // Assert
            var registeredNames = await _agentFactory.GetRegisteredAgentNamesAsync();
            Assert.Contains("MinimalTestAgent", registeredNames);
        }

        [Fact]
        public async Task AgentFactory_GetAvailableAgentTypes_ReturnsRegisteredTypes()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(MinimalTestAgent));

            // Act
            var availableTypes = await _agentFactory.GetAvailableAgentTypesAsync();

            // Assert
            Assert.Contains(typeof(MinimalTestAgent), availableTypes);
        }

        [Fact]
        public async Task AgentFactory_IsAgentRegistered_ReturnsCorrectStatus()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(MinimalTestAgent));

            // Act & Assert
            Assert.True(await _agentFactory.IsAgentRegisteredAsync("MinimalTestAgent"));
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("NonExistentAgent"));
        }

        [Fact]
        public async Task FactoryAndDiscovery_FullWorkflow_Success()
        {
            // Arrange & Act
            // 1. Discover agents
            var agentTypes = await _discoveryService.DiscoverAgentsAsync(Assembly.GetExecutingAssembly());
            Assert.NotEmpty(agentTypes);

            // 2. Auto-register discovered agents
            await _discoveryService.AutoRegisterAgentsAsync(_agentFactory, Assembly.GetExecutingAssembly());

            // 3. Create agent instance
            var agent = await _agentFactory.CreateAgentAsync("MinimalTestAgent");
            Assert.NotNull(agent);

            // 4. Test agent functionality
            var request = new AgentRequest("Test request");
            var result = await agent.HandleAsync(request);
            Assert.True(result.Success);

            // 5. Verify agent can handle requests
            var canHandle = await agent.CanHandleAsync(request);
            Assert.True(canHandle);
        }

        [Fact]
        public async Task AgentFactory_UnregisterAgent_Success()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(MinimalTestAgent));
            Assert.True(await _agentFactory.IsAgentRegisteredAsync("MinimalTestAgent"));

            // Act
            await _agentFactory.UnregisterAgentTypeAsync("MinimalTestAgent");

            // Assert
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("MinimalTestAgent"));
        }

        [Fact]
        public async Task AgentFactory_RegisterWithCustomName_Success()
        {
            // Arrange
            var customName = "CustomMinimalAgent";

            // Act
            await _agentFactory.RegisterAgentTypeAsync(typeof(MinimalTestAgent), customName);

            // Assert
            Assert.True(await _agentFactory.IsAgentRegisteredAsync(customName));
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("MinimalTestAgent"));

            var agent = await _agentFactory.CreateAgentAsync(customName);
            Assert.NotNull(agent);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}