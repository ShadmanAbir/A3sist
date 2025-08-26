using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Enhanced agent interface with lifecycle management and request filtering capabilities
    /// </summary>
    public interface IAgent
    {
        /// <summary>
        /// Gets the unique name of the agent
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type of the agent
        /// </summary>
        AgentType Type { get; }

        /// <summary>
        /// Handles an agent request asynchronously
        /// </summary>
        /// <param name="request">The request to handle</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The result of handling the request</returns>
        Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if this agent can handle the specified request
        /// </summary>
        /// <param name="request">The request to evaluate</param>
        /// <returns>True if the agent can handle the request, false otherwise</returns>
        Task<bool> CanHandleAsync(AgentRequest request);

        /// <summary>
        /// Initializes the agent asynchronously
        /// </summary>
        /// <returns>A task representing the initialization operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the agent asynchronously
        /// </summary>
        /// <returns>A task representing the shutdown operation</returns>
        Task ShutdownAsync();
    }
}