using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for LLM (Large Language Model) clients
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Sends a completion request to the LLM
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The completion response</returns>
        Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a completion request with options
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <param name="options">Additional options for the request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The completion response</returns>
        Task<string> CompleteAsync(string prompt, Dictionary<string, object>? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a streaming completion request
        /// </summary>
        /// <param name="prompt">The prompt to send</param>
        /// <param name="onChunk">Callback for each response chunk</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the streaming operation</returns>
        Task CompleteStreamAsync(string prompt, Action<string> onChunk, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the available models for this client
        /// </summary>
        /// <returns>List of available model names</returns>
        Task<IEnumerable<string>> GetAvailableModelsAsync();

        /// <summary>
        /// Gets the current model being used
        /// </summary>
        string CurrentModel { get; }

        /// <summary>
        /// Gets whether the client is currently available
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Initializes the LLM client
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Disposes the LLM client resources
        /// </summary>
        Task DisposeAsync();
    }
}