using A3sist.Shared.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for the main orchestrator that coordinates agent interactions
    /// </summary>
    public interface IOrchestrator
    {
        /// <summary>
        /// Processes a request by routing it to appropriate agents
        /// </summary>
        /// <param name="request">The request to process</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The result of processing the request</returns>
        Task<AgentResult> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available agents
        /// </summary>
        /// <returns>Collection of available agents</returns>
        Task<IEnumerable<IAgent>> GetAvailableAgentsAsync();

        /// <summary>
        /// Registers an agent with the orchestrator
        /// </summary>
        /// <param name="agent">The agent to register</param>
        /// <returns>Task representing the registration operation</returns>
        Task RegisterAgentAsync(IAgent agent);

        /// <summary>
        /// Unregisters an agent from the orchestrator
        /// </summary>
        /// <param name="agentName">The name of the agent to unregister</param>
        /// <returns>Task representing the unregistration operation</returns>
        Task UnregisterAgentAsync(string agentName);

        /// <summary>
        /// Initializes the orchestrator
        /// </summary>
        /// <returns>Task representing the initialization operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the orchestrator
        /// </summary>
        /// <returns>Task representing the shutdown operation</returns>
        Task ShutdownAsync();
    }
}