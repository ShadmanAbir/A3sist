using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for external documentation search services
    /// </summary>
    public interface IDocumentationSearchService
    {
        /// <summary>
        /// Search external documentation sources
        /// </summary>
        Task<IEnumerable<KnowledgeEntry>> SearchDocumentationAsync(
            string query, 
            KnowledgeContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Search specific documentation source
        /// </summary>
        Task<IEnumerable<KnowledgeEntry>> SearchSourceAsync(
            string query, 
            string source, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get available documentation sources
        /// </summary>
        Task<IEnumerable<string>> GetAvailableSourcesAsync();
    }
}