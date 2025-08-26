using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Provider for specific types of code fixes
    /// </summary>
    public interface IFixProvider
    {
        /// <summary>
        /// Name of the fix provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Language supported by this provider
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Checks if this provider can handle the given code issue
        /// </summary>
        /// <param name="codeInfo">Code information</param>
        /// <returns>True if provider can handle the issue</returns>
        bool CanHandle(CodeInfo codeInfo);

        /// <summary>
        /// Provides fixes for the given code
        /// </summary>
        /// <param name="codeInfo">Code information to fix</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available fixes</returns>
        Task<IEnumerable<CodeFix>> ProvideFixesAsync(CodeInfo codeInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes the fix provider
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the fix provider
        /// </summary>
        Task ShutdownAsync();
    }
}