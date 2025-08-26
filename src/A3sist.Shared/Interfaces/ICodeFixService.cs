using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Service for applying code fixes
    /// </summary>
    public interface ICodeFixService
    {
        /// <summary>
        /// Applies fixes to the provided code
        /// </summary>
        /// <param name="codeInfo">Code information to fix</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Fixed code result</returns>
        Task<CodeFixResult> ApplyFixesAsync(CodeInfo codeInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets available code fixes for the provided code
        /// </summary>
        /// <param name="codeInfo">Code information to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available code fixes</returns>
        Task<IEnumerable<CodeFix>> GetAvailableFixesAsync(CodeInfo codeInfo, CancellationToken cancellationToken = default);
    }
}