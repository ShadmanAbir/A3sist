using A3sist.Core.Agents.Base;
using A3sist.Core.Services;
using A3sist.Shared.Attributes;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Integration.Tests
{
    public class AgentRegistrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IAgentFactory _agentFactory;
        private readonly IAgentDiscoveryService _discoveryService;
        private readonly IAgentManager _agentManager;

        public AgentRegistrationTests()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Add our services
            services.AddSingleton<IAgentFactory, AgentFactory>();
            services.AddSingleton<IAgentDiscoveryService, AgentDiscoveryService>();
            services.AddSingleton<IAgentManager, AgentManager>();
            
            // Add mock configuration
            var mockConfig = new Mock<IAgentConfiguration>();
            services.AddSingleton(mockConfig.Object);
            
            _serviceProvider = services.BuildServiceProvider();
            _agentFactory = _serviceProvider.GetRequiredService<IAgentFactory>();
            _discoveryService = _serviceProvider.GetRequiredService<IAgentDiscoveryService>();
            _agentManager = _serviceProvider.GetRequiredService<IAgentManager>();
        }

        [Fact]
        public async Task AgentFactory_RegisterAndCreateAgent_Success()
        {
            // Arrange
            var agentType = typeof(TestIntegrationAgent);

            // Act
            await _agentFactory.RegisterAgentTypeAsync(agentType);
            var agent = await _agentFactory.CreateAgentAsync("TestIntegrationAgent");

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("TestIntegrationAgent", agent.Name);
            Assert.Equal(AgentType.Unknown, agent.Type);
        }

        [Fact]
        public async Task AgentFactory_CreateAgentByType_Success()
        {
            // Arrange
            var agentType = typeof(TestIntegrationAgent);
            await _agentFactory.RegisterAgentTypeAsync(agentType);

            // Act
            var agent = await _agentFactory.CreateAgentAsync(AgentType.Unknown);

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("TestIntegrationAgent", agent.Name);
        }

        [Fact]
        public async Task AgentDiscoveryService_DiscoverAgents_FindsTestAgent()
        {
            // Act
            var agentTypes = await _discoveryService.DiscoverAgentsAsync(Assembly.GetExecutingAssembly());

            // Assert
            Assert.Contains(typeof(TestIntegrationAgent), agentTypes);
        }

        [Fact]
        public async Task AgentDiscoveryService_GetAgentMetadata_ReturnsCorrectMetadata()
        {
            // Act
            var metadata = await _discoveryService.GetAgentMetadataAsync(typeof(TestIntegrationAgent));

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal("TestIntegrationAgent", metadata.Name);
            Assert.Equal(AgentType.Unknown, metadata.Type);
            Assert.Contains("test", metadata.Keywords);
            Assert.Contains(".test", metadata.SupportedFileExtensions);
        }

        [Fact]
        public async Task AgentDiscoveryService_ValidateAgentType_ReturnsValid()
        {
            // Act
            var result = await _discoveryService.ValidateAgentTypeAsync(typeof(TestIntegrationAgent));

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
            Assert.Contains("TestIntegrationAgent", registeredNames);
        }

        [Fact]
        public async Task FullIntegration_DiscoverRegisterAndManage_Success()
        {
            // Arrange & Act
            // 1. Discover agents
            var agentTypes = await _discoveryService.DiscoverAgentsAsync(Assembly.GetExecutingAssembly());
            Assert.NotEmpty(agentTypes);

            // 2. Auto-register discovered agents
            await _discoveryService.AutoRegisterAgentsAsync(_agentFactory, Assembly.GetExecutingAssembly());

            // 3. Create agent instance
            var agent = await _agentFactory.CreateAgentAsync("TestIntegrationAgent");
            Assert.NotNull(agent);

            // 4. Register with agent manager
            await _agentManager.RegisterAgentAsync(agent);

            // 5. Verify agent is managed
            var managedAgent = await _agentManager.GetAgentAsync("TestIntegrationAgent");
            Assert.NotNull(managedAgent);

            // 6. Get agent status
            var status = await _agentManager.GetAgentStatusAsync("TestIntegrationAgent");
            Assert.NotNull(status);
            Assert.Equal("TestIntegrationAgent", status.Name);

            // 7. Test agent functionality
            var request = new AgentRequest("Test request");
            var result = await managedAgent.HandleAsync(request);
            Assert.True(result.Success);

            // 8. Unregister agent
            await _agentManager.UnregisterAgentAsync("TestIntegrationAgent");
            var unregisteredAgent = await _agentManager.GetAgentAsync("TestIntegrationAgent");
            Assert.Null(unregisteredAgent);
        }

        [Fact]
        public async Task AgentFactory_GetAvailableAgentTypes_ReturnsRegisteredTypes()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestIntegrationAgent));

            // Act
            var availableTypes = await _agentFactory.GetAvailableAgentTypesAsync();

            // Assert
            Assert.Contains(typeof(TestIntegrationAgent), availableTypes);
        }

        [Fact]
        public async Task AgentFactory_IsAgentRegistered_ReturnsCorrectStatus()
        {
            // Arrange
            await _agentFactory.RegisterAgentTypeAsync(typeof(TestIntegrationAgent));

            // Act & Assert
            Assert.True(await _agentFactory.IsAgentRegisteredAsync("TestIntegrationAgent"));
            Assert.False(await _agentFactory.IsAgentRegisteredAsync("NonExistentAgent"));
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Test agent for integration testing
    /// </summary>
    [AgentCapability("TestCapability", 
        Description = "Test capability for integration testing",
        AgentType = AgentType.Unknown,
        Keywords = "test,integration",
        FileExtensions = ".test,.integration")]
    public class TestIntegrationAgent : BaseAgent
    {
        public override string Name => "TestIntegrationAgent";
        public override AgentType Type => AgentType.Unknown;

        public TestIntegrationAgent(ILogger<TestIntegrationAgent> logger, IAgentConfiguration configuration)
            : base(logger, configuration)
        {
        }

        protected override async Task<AgentResult> HandleRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            return AgentResult.CreateSuccess("Integration test completed", "Test result");
        }

        protected override Task<bool> CanHandleRequestAsync(AgentRequest request)
        {
            return Task.FromResult(request.Prompt?.Contains("test", StringComparison.OrdinalIgnoreCase) == true);
        }
    }
}