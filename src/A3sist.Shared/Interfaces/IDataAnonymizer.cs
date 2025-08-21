using A3sist.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for anonymizing training data
    /// </summary>
    public interface IDataAnonymizer
    {
        /// <summary>
        /// Anonymize a single agent interaction
        /// </summary>
        Task<AgentInteraction> AnonymizeInteractionAsync(
            AgentInteraction interaction, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Anonymize text content by removing PII
        /// </summary>
        Task<string> AnonymizeTextAsync(
            string text, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate anonymous user ID from original user ID
        /// </summary>
        string AnonymizeUserId(string originalUserId);

        /// <summary>
        /// Anonymize file paths by removing personal directories
        /// </summary>
        string AnonymizeFilePath(string filePath);

        /// <summary>
        /// Check if text contains personally identifiable information
        /// </summary>
        Task<bool> ContainsPiiAsync(
            string text, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get anonymization statistics
        /// </summary>
        Task<AnonymizationStatistics> GetStatisticsAsync(
            CancellationToken cancellationToken = default);
    }
}