using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Service for compiler diagnostics and analysis
    /// </summary>
    public interface ICompilerDiagnosticsService
    {
        /// <summary>
        /// Gets diagnostics for the provided code
        /// </summary>
        /// <param name="codeInfo">Code information to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of diagnostics</returns>
        Task<IEnumerable<CodeDiagnostic>> GetDiagnosticsAsync(CodeInfo codeInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes the diagnostics service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the diagnostics service
        /// </summary>
        Task ShutdownAsync();
    }
}