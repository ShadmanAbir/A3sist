using A3sist.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for privacy filtering of training data
    /// </summary>
    public interface IPrivacyFilter
    {
        /// <summary>
        /// Determine if an interaction should be collected for training
        /// </summary>
        Task<bool> ShouldCollectAsync(
            AgentInteraction interaction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determine if an interaction should be included in exports
        /// </summary>
        Task<bool> ShouldIncludeInExportAsync(
            AgentInteraction interaction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if user has consented to data collection
        /// </summary>
        Task<bool> HasUserConsentAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Apply privacy rules to interaction content
        /// </summary>
        Task<AgentInteraction> ApplyPrivacyRulesAsync(
            AgentInteraction interaction,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get privacy compliance status
        /// </summary>
        Task<PrivacyComplianceStatus> GetComplianceStatusAsync(
            CancellationToken cancellationToken = default);
    }
}