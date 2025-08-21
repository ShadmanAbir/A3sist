using A3sist.Core.Agents.Base;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Utility.TrainingData
{
    /// <summary>
    /// Agent responsible for collecting and generating training data from agent interactions
    /// </summary>
    public class TrainingDataGenerator : BaseAgent
    {
        private readonly ITrainingDataRepository _dataRepository;
        private readonly IDataAnonymizer _dataAnonymizer;
        private readonly ITrainingDataExporter _dataExporter;
        private readonly IPrivacyFilter _privacyFilter;
        private readonly Queue<AgentInteraction> _interactionQueue;
        private readonly object _queueLock = new object();
        private Timer _processingTimer;

        public override string Name => "TrainingDataGenerator";
        public override AgentType Type => AgentType.Utility;

        public TrainingDataGenerator(
            ILogger<TrainingDataGenerator> logger,
            IAgentConfiguration configuration,
            ITrainingDataRepository dataRepository,
            IDataAnonymizer dataAnonymizer,
            ITrainingDataExporter dataExporter,
            IPrivacyFilter privacyFilter)
            : base(logger, configuration)
        {
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _dataAnonymizer = dataAnonymizer ?? throw new ArgumentNullException(nameof(dataAnonymizer));
            _dataExporter = dataExporter ?? throw new ArgumentNullException(nameof(dataExporter));
            _privacyFilter = privacyFilter ?? throw new ArgumentNullException(nameof(privacyFilter));
            _interactionQueue = new Queue<AgentInteraction>();
        }

        public override async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request?.Prompt == null) return false;

            var prompt = request.Prompt.ToLowerInvariant();
            
            // Training data related keywords
            var trainingKeywords = new[]
            {
                "training data", "export data", "generate dataset", "collect interactions",
                "anonymize data", "data export", "learning data", "training set",
                "interaction history", "usage analytics"
            };

            return trainingKeywords.Any(keyword => prompt.Contains(keyword));
        }

        public override async Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Processing training data request: {RequestId}", request.Id);

                var requestType = DetermineRequestType(request.Prompt);
                
                return requestType switch
                {
                    TrainingDataRequestType.Export => await HandleExportRequestAsync(request, cancellationToken),
                    TrainingDataRequestType.Collect => await HandleCollectionRequestAsync(request, cancellationToken),
                    TrainingDataRequestType.Anonymize => await HandleAnonymizationRequestAsync(request, cancellationToken),
                    TrainingDataRequestType.Statistics => await HandleStatisticsRequestAsync(request, cancellationToken),
                    _ => await HandleGeneralRequestAsync(request, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing training data request: {RequestId}", request.Id);
                return new AgentResult
                {
                    Success = false,
                    Message = $"Failed to process training data request: {ex.Message}",
                    Exception = ex,
                    AgentName = Name
                };
            }
        }

        private async Task<AgentResult> HandleExportRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var exportOptions = ExtractExportOptions(request);
            var interactions = await _dataRepository.GetInteractionsAsync(exportOptions.Filter, cancellationToken);
            
            // Apply privacy filtering
            var filteredInteractions = await ApplyPrivacyFiltersAsync(interactions, cancellationToken);
            
            // Anonymize data
            var anonymizedInteractions = await AnonymizeInteractionsAsync(filteredInteractions, cancellationToken);
            
            // Export data
            var exportResult = await _dataExporter.ExportAsync(anonymizedInteractions, exportOptions, cancellationToken);
            
            return new AgentResult
            {
                Success = exportResult.Success,
                Content = GenerateExportSummary(exportResult),
                Message = exportResult.Success ? "Training data exported successfully" : "Export failed",
                AgentName = Name,
                Metadata = new Dictionary<string, object>
                {
                    ["ExportedCount"] = anonymizedInteractions.Count(),
                    ["ExportFormat"] = exportOptions.Format.ToString(),
                    ["ExportPath"] = exportResult.FilePath,
                    ["DataSize"] = exportResult.DataSize
                }
            };
        }

        private async Task<AgentResult> HandleCollectionRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var collectionOptions = ExtractCollectionOptions(request);
            
            // Start or configure data collection
            await ConfigureDataCollectionAsync(collectionOptions, cancellationToken);
            
            var currentStats = await _dataRepository.GetCollectionStatisticsAsync(cancellationToken);
            
            return new AgentResult
            {
                Success = true,
                Content = GenerateCollectionStatus(currentStats, collectionOptions),
                Message = "Data collection configured successfully",
                AgentName = Name,
                Metadata = new Dictionary<string, object>
                {
                    ["CollectionEnabled"] = collectionOptions.Enabled,
                    ["TotalInteractions"] = currentStats.TotalInteractions,
                    ["CollectionRate"] = currentStats.CollectionRate
                }
            };
        }

        private async Task<AgentResult> HandleAnonymizationRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var anonymizationOptions = ExtractAnonymizationOptions(request);
            var interactions = await _dataRepository.GetInteractionsAsync(anonymizationOptions.Filter, cancellationToken);
            
            var anonymizedInteractions = await AnonymizeInteractionsAsync(interactions, cancellationToken);
            
            // Store anonymized data
            await _dataRepository.StoreAnonymizedInteractionsAsync(anonymizedInteractions, cancellationToken);
            
            return new AgentResult
            {
                Success = true,
                Content = GenerateAnonymizationSummary(interactions.Count(), anonymizedInteractions.Count()),
                Message = "Data anonymization completed successfully",
                AgentName = Name,
                Metadata = new Dictionary<string, object>
                {
                    ["OriginalCount"] = interactions.Count(),
                    ["AnonymizedCount"] = anonymizedInteractions.Count(),
                    ["AnonymizationRate"] = (double)anonymizedInteractions.Count() / interactions.Count()
                }
            };
        }

        private async Task<AgentResult> HandleStatisticsRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var stats = await _dataRepository.GetDetailedStatisticsAsync(cancellationToken);
            
            return new AgentResult
            {
                Success = true,
                Content = GenerateStatisticsReport(stats),
                Message = "Training data statistics retrieved successfully",
                AgentName = Name,
                Metadata = new Dictionary<string, object>
                {
                    ["TotalInteractions"] = stats.TotalInteractions,
                    ["UniqueUsers"] = stats.UniqueUsers,
                    ["DataSizeBytes"] = stats.TotalDataSize,
                    ["CollectionPeriod"] = stats.CollectionPeriod.ToString()
                }
            };
        }

        private async Task<AgentResult> HandleGeneralRequestAsync(AgentRequest request, CancellationToken cancellationToken)
        {
            var capabilities = GetCapabilities();
            
            return new AgentResult
            {
                Success = true,
                Content = GenerateCapabilitiesDescription(capabilities),
                Message = "Training data generator capabilities",
                AgentName = Name,
                Metadata = new Dictionary<string, object>
                {
                    ["AvailableFormats"] = capabilities.SupportedFormats,
                    ["PrivacyFeatures"] = capabilities.PrivacyFeatures,
                    ["ExportOptions"] = capabilities.ExportOptions
                }
            };
        }

        public async Task RecordInteractionAsync(AgentInteraction interaction)
        {
            try
            {
                // Apply privacy filtering immediately
                if (!await _privacyFilter.ShouldCollectAsync(interaction))
                {
                    Logger.LogDebug("Interaction filtered out by privacy filter: {InteractionId}", interaction.Id);
                    return;
                }

                lock (_queueLock)
                {
                    _interactionQueue.Enqueue(interaction);
                }

                Logger.LogDebug("Interaction queued for processing: {InteractionId}", interaction.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error recording interaction: {InteractionId}", interaction.Id);
            }
        }

        private async Task ProcessQueuedInteractionsAsync()
        {
            var interactions = new List<AgentInteraction>();
            
            lock (_queueLock)
            {
                while (_interactionQueue.Count > 0)
                {
                    interactions.Add(_interactionQueue.Dequeue());
                }
            }

            if (!interactions.Any()) return;

            try
            {
                // Anonymize interactions
                var anonymizedInteractions = await AnonymizeInteractionsAsync(interactions, CancellationToken.None);
                
                // Store in repository
                await _dataRepository.StoreInteractionsAsync(anonymizedInteractions, CancellationToken.None);
                
                Logger.LogInformation("Processed {Count} interactions for training data", interactions.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing queued interactions");
                
                // Re-queue interactions for retry
                lock (_queueLock)
                {
                    foreach (var interaction in interactions)
                    {
                        _interactionQueue.Enqueue(interaction);
                    }
                }
            }
        }

        private async Task<IEnumerable<AgentInteraction>> ApplyPrivacyFiltersAsync(
            IEnumerable<AgentInteraction> interactions, 
            CancellationToken cancellationToken)
        {
            var filteredInteractions = new List<AgentInteraction>();
            
            foreach (var interaction in interactions)
            {
                if (await _privacyFilter.ShouldIncludeInExportAsync(interaction, cancellationToken))
                {
                    filteredInteractions.Add(interaction);
                }
            }
            
            return filteredInteractions;
        }

        private async Task<IEnumerable<AgentInteraction>> AnonymizeInteractionsAsync(
            IEnumerable<AgentInteraction> interactions, 
            CancellationToken cancellationToken)
        {
            var anonymizedInteractions = new List<AgentInteraction>();
            
            foreach (var interaction in interactions)
            {
                var anonymized = await _dataAnonymizer.AnonymizeInteractionAsync(interaction, cancellationToken);
                anonymizedInteractions.Add(anonymized);
            }
            
            return anonymizedInteractions;
        }

        private TrainingDataRequestType DetermineRequestType(string prompt)
        {
            var lowerPrompt = prompt.ToLowerInvariant();
            
            if (lowerPrompt.Contains("export") || lowerPrompt.Contains("download"))
                return TrainingDataRequestType.Export;
            if (lowerPrompt.Contains("collect") || lowerPrompt.Contains("start") || lowerPrompt.Contains("enable"))
                return TrainingDataRequestType.Collect;
            if (lowerPrompt.Contains("anonymize") || lowerPrompt.Contains("privacy"))
                return TrainingDataRequestType.Anonymize;
            if (lowerPrompt.Contains("statistics") || lowerPrompt.Contains("stats") || lowerPrompt.Contains("report"))
                return TrainingDataRequestType.Statistics;
                
            return TrainingDataRequestType.General;
        }

        private TrainingDataExportOptions ExtractExportOptions(AgentRequest request)
        {
            // Simple extraction - could be enhanced with NLP
            var options = new TrainingDataExportOptions();
            var prompt = request.Prompt.ToLowerInvariant();
            
            if (prompt.Contains("json")) options.Format = ExportFormat.Json;
            else if (prompt.Contains("csv")) options.Format = ExportFormat.Csv;
            else if (prompt.Contains("xml")) options.Format = ExportFormat.Xml;
            
            if (prompt.Contains("last week")) options.Filter.DateRange = DateRange.LastWeek;
            else if (prompt.Contains("last month")) options.Filter.DateRange = DateRange.LastMonth;
            
            return options;
        }

        private DataCollectionOptions ExtractCollectionOptions(AgentRequest request)
        {
            var options = new DataCollectionOptions();
            var prompt = request.Prompt.ToLowerInvariant();
            
            options.Enabled = !prompt.Contains("disable") && !prompt.Contains("stop");
            
            return options;
        }

        private DataAnonymizationOptions ExtractAnonymizationOptions(AgentRequest request)
        {
            return new DataAnonymizationOptions
            {
                RemovePersonalInfo = true,
                HashUserIds = true,
                RemoveFilePaths = true,
                GeneralizeTimestamps = true
            };
        }

        private string GenerateExportSummary(TrainingDataExportResult result)
        {
            if (!result.Success)
            {
                return $"Export failed: {result.ErrorMessage}";
            }
            
            return $"**Training Data Export Complete**\n\n" +
                   $"- **File:** {result.FilePath}\n" +
                   $"- **Format:** {result.Format}\n" +
                   $"- **Records:** {result.RecordCount:N0}\n" +
                   $"- **Size:** {FormatBytes(result.DataSize)}\n" +
                   $"- **Export Time:** {result.ExportTime:F2}s\n\n" +
                   $"The exported data has been anonymized and is ready for training purposes.";
        }

        private string GenerateCollectionStatus(CollectionStatistics stats, DataCollectionOptions options)
        {
            return $"**Training Data Collection Status**\n\n" +
                   $"- **Collection:** {(options.Enabled ? "✅ Enabled" : "❌ Disabled")}\n" +
                   $"- **Total Interactions:** {stats.TotalInteractions:N0}\n" +
                   $"- **Collection Rate:** {stats.CollectionRate:F1} interactions/hour\n" +
                   $"- **Data Size:** {FormatBytes(stats.TotalDataSize)}\n" +
                   $"- **Collection Period:** {stats.CollectionPeriod.TotalDays:F0} days\n\n" +
                   $"Data collection is configured to respect privacy settings and anonymize all personal information.";
        }

        private string GenerateAnonymizationSummary(int originalCount, int anonymizedCount)
        {
            var successRate = originalCount > 0 ? (double)anonymizedCount / originalCount * 100 : 0;
            
            return $"**Data Anonymization Complete**\n\n" +
                   $"- **Original Records:** {originalCount:N0}\n" +
                   $"- **Anonymized Records:** {anonymizedCount:N0}\n" +
                   $"- **Success Rate:** {successRate:F1}%\n\n" +
                   $"All personal information has been removed or anonymized according to privacy policies.";
        }

        private string GenerateStatisticsReport(DetailedStatistics stats)
        {
            return $"**Training Data Statistics**\n\n" +
                   $"**Collection Overview:**\n" +
                   $"- Total Interactions: {stats.TotalInteractions:N0}\n" +
                   $"- Unique Users: {stats.UniqueUsers:N0}\n" +
                   $"- Collection Period: {stats.CollectionPeriod.TotalDays:F0} days\n" +
                   $"- Data Size: {FormatBytes(stats.TotalDataSize)}\n\n" +
                   $"**Agent Distribution:**\n" +
                   string.Join("\n", stats.AgentDistribution.Select(kvp => $"- {kvp.Key}: {kvp.Value:N0}")) + "\n\n" +
                   $"**Quality Metrics:**\n" +
                   $"- Success Rate: {stats.SuccessRate:F1}%\n" +
                   $"- Average Response Time: {stats.AverageResponseTime:F2}s\n" +
                   $"- Data Quality Score: {stats.DataQualityScore:F1}/10";
        }

        private string GenerateCapabilitiesDescription(TrainingDataCapabilities capabilities)
        {
            return $"**Training Data Generator Capabilities**\n\n" +
                   $"**Data Collection:**\n" +
                   $"- Automatic interaction recording\n" +
                   $"- Privacy-compliant data filtering\n" +
                   $"- Real-time anonymization\n\n" +
                   $"**Export Formats:**\n" +
                   string.Join("\n", capabilities.SupportedFormats.Select(f => $"- {f}")) + "\n\n" +
                   $"**Privacy Features:**\n" +
                   string.Join("\n", capabilities.PrivacyFeatures.Select(f => $"- {f}")) + "\n\n" +
                   $"**Available Commands:**\n" +
                   $"- Export training data\n" +
                   $"- View collection statistics\n" +
                   $"- Configure data collection\n" +
                   $"- Anonymize existing data";
        }

        private TrainingDataCapabilities GetCapabilities()
        {
            return new TrainingDataCapabilities
            {
                SupportedFormats = new[] { "JSON", "CSV", "XML", "Parquet" },
                PrivacyFeatures = new[] { "PII Removal", "Data Anonymization", "User Consent Tracking", "GDPR Compliance" },
                ExportOptions = new[] { "Date Range Filtering", "Agent Type Filtering", "Quality Filtering", "Custom Schemas" }
            };
        }

        private async Task ConfigureDataCollectionAsync(DataCollectionOptions options, CancellationToken cancellationToken)
        {
            await _dataRepository.UpdateCollectionSettingsAsync(options, cancellationToken);
            
            if (options.Enabled && _processingTimer == null)
            {
                _processingTimer = new Timer(
                    async _ => await ProcessQueuedInteractionsAsync(),
                    null,
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5));
            }
            else if (!options.Enabled && _processingTimer != null)
            {
                _processingTimer?.Dispose();
                _processingTimer = null;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Logger.LogInformation("TrainingDataGenerator initialized");
            
            // Initialize data repository
            await _dataRepository.InitializeAsync();
            
            // Start processing timer if collection is enabled
            var settings = await _dataRepository.GetCollectionSettingsAsync();
            if (settings.Enabled)
            {
                _processingTimer = new Timer(
                    async _ => await ProcessQueuedInteractionsAsync(),
                    null,
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(5));
            }
        }

        public override async Task ShutdownAsync()
        {
            Logger.LogInformation("TrainingDataGenerator shutting down");
            
            // Stop processing timer
            _processingTimer?.Dispose();
            
            // Process any remaining queued interactions
            await ProcessQueuedInteractionsAsync();
            
            await base.ShutdownAsync();
        }
    }
}