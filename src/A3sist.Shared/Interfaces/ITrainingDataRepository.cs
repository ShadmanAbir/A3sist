using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for training data repository operations
    /// </summary>
    public interface ITrainingDataRepository
    {
        /// <summary>
        /// Initialize the training data repository
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Store agent interactions for training data
        /// </summary>
        Task StoreInteractionsAsync(
            IEnumerable<AgentInteraction> interactions, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Store anonymized interactions
        /// </summary>
        Task StoreAnonymizedInteractionsAsync(
            IEnumerable<AgentInteraction> interactions, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get interactions based on filter criteria
        /// </summary>
        Task<IEnumerable<AgentInteraction>> GetInteractionsAsync(
            TrainingDataFilter filter, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get collection statistics
        /// </summary>
        Task<CollectionStatistics> GetCollectionStatisticsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed statistics for reporting
        /// </summary>
        Task<DetailedStatistics> GetDetailedStatisticsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update data collection settings
        /// </summary>
        Task UpdateCollectionSettingsAsync(
            DataCollectionOptions options, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current collection settings
        /// </summary>
        Task<DataCollectionOptions> GetCollectionSettingsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete interactions older than specified date
        /// </summary>
        Task CleanupOldInteractionsAsync(
            System.DateTime cutoffDate, 
            CancellationToken cancellationToken = default);
    }
}