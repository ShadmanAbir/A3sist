using A3sist.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for sandboxing shell command execution
    /// </summary>
    public interface ICommandSandbox
    {
        /// <summary>
        /// Prepare a command for safe execution in a sandboxed environment
        /// </summary>
        Task<SandboxedCommand> PrepareCommandAsync(
            ShellCommand command, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Set up the sandbox environment
        /// </summary>
        Task InitializeSandboxAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clean up the sandbox environment
        /// </summary>
        Task CleanupSandboxAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the sandbox is properly configured
        /// </summary>
        Task<bool> IsSandboxReadyAsync();
    }
}