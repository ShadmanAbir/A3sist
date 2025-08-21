using A3sist.Shared.Attributes;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Integration.Tests.TestAgents
{
    /// <summary>
    /// Minimal test agent for testing the factory and registration system
    /// </summary>
    [AgentCapability("MinimalTest", 
        Description = "Minimal test agent for factory testing",
        AgentType = AgentType.Unknown,
        Keywords = "test,minimal",
        FileExtensions = ".test")]
    public class MinimalTestAgent : IAgent
    {
        private readonly ILogger<MinimalTestAgent> _logger;
        private readonly IAgentConfiguration _configuration;

        public string Name => "MinimalTestAgent";
        public AgentType Type => AgentType.Unknown;

        public MinimalTestAgent(ILogger<MinimalTestAgent> logger, IAgentConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MinimalTestAgent handling request: {Prompt}", request.Prompt);
            return Task.FromResult(AgentResult.CreateSuccess("Minimal test completed", "Test result"));
        }

        public Task<bool> CanHandleAsync(AgentRequest request)
        {
            return Task.FromResult(request.Prompt?.Contains("test", StringComparison.OrdinalIgnoreCase) == true);
        }

        public Task InitializeAsync()
        {
            _logger.LogInformation("MinimalTestAgent initialized");
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            _logger.LogInformation("MinimalTestAgent shutdown");
            return Task.CompletedTask;
        }
    }
}