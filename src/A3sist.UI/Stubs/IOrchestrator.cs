using System;
using System.Threading;
using System.Threading.Tasks;
using A3sist.Shared.Messaging;

namespace A3sist.Core.Services
{
    /// <summary>
    /// Stub interface for IOrchestrator to allow UI project to compile
    /// This is a temporary stub for task 10.1 implementation
    /// </summary>
    public interface IOrchestrator
    {
        /// <summary>
        /// Process an agent request
        /// </summary>
        Task<AgentResult> ProcessRequestAsync(AgentRequest request, CancellationToken cancellationToken = default);
    }
}