using A3sist.Shared.Models;
using A3sist.Shared.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Service for validating requests and data
    /// </summary>
    public interface IValidationService
    {
        /// <summary>
        /// Validates an agent request
        /// </summary>
        /// <param name="request">The request to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateRequestAsync(AgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates configuration data
        /// </summary>
        /// <param name="configuration">The configuration to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateConfigurationAsync(object configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates code content
        /// </summary>
        /// <param name="code">The code to validate</param>
        /// <param name="language">The programming language</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateCodeAsync(string code, string language, CancellationToken cancellationToken = default);
    }
}