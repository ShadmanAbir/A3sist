using A3sist.Shared.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for exporting training data in various formats
    /// </summary>
    public interface ITrainingDataExporter
    {
        /// <summary>
        /// Export training data to specified format
        /// </summary>
        Task<TrainingDataExportResult> ExportAsync(
            IEnumerable<AgentInteraction> interactions,
            TrainingDataExportOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get supported export formats
        /// </summary>
        Task<IEnumerable<ExportFormat>> GetSupportedFormatsAsync();

        /// <summary>
        /// Validate export options
        /// </summary>
        Task<ExportValidationResult> ValidateExportOptionsAsync(
            TrainingDataExportOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get export schema for specified format
        /// </summary>
        Task<string> GetExportSchemaAsync(
            ExportFormat format,
            CancellationToken cancellationToken = default);
    }
}