using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for knowledge base repository operations
    /// </summary>
    public interface IKnowledgeRepository
    {
        /// <summary>
        /// Initialize the knowledge repository
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Search for knowledge entries based on query and context
        /// </summary>
        Task<IEnumerable<KnowledgeEntry>> SearchAsync(
            string query, 
            KnowledgeContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add or update a knowledge entry
        /// </summary>
        Task<bool> UpsertEntryAsync(
            KnowledgeEntry entry, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Record a knowledge interaction for learning
        /// </summary>
        Task RecordInteractionAsync(
            KnowledgeInteraction interaction, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get knowledge entries by category
        /// </summary>
        Task<IEnumerable<KnowledgeEntry>> GetByCategoryAsync(
            string category, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get related knowledge entries
        /// </summary>
        Task<IEnumerable<KnowledgeEntry>> GetRelatedAsync(
            string entryId, 
            CancellationToken cancellationToken = default);
    }
}