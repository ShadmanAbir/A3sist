using A3sist.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for validating shell commands before execution
    /// </summary>
    public interface ICommandValidator
    {
        /// <summary>
        /// Validate a shell command for safety and security
        /// </summary>
        Task<CommandValidationResult> ValidateAsync(
            ShellCommand command, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a command is in the allowed list
        /// </summary>
        bool IsCommandAllowed(string command);

        /// <summary>
        /// Check if a command is in the blocked list
        /// </summary>
        bool IsCommandBlocked(string command);

        /// <summary>
        /// Assess the security risk level of a command
        /// </summary>
        SecurityRiskLevel AssessSecurityRisk(ShellCommand command);
    }
}