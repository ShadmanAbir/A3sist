using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents an agent interaction for training data
    /// </summary>
    public class AgentInteraction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string AgentType { get; set; } = string.Empty;
        public string Request { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan ProcessingTime { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public bool IsAnonymized { get; set; }
        public string OriginalHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Options for training data export
    /// </summary>
    public class TrainingDataExportOptions
    {
        public ExportFormat Format { get; set; } = ExportFormat.Json;
        public string OutputPath { get; set; } = string.Empty;
        public TrainingDataFilter Filter { get; set; } = new TrainingDataFilter();
        public bool IncludeMetadata { get; set; } = true;
        public bool CompressOutput { get; set; } = false;
        public int MaxRecords { get; set; } = 10000;
        public string SchemaVersion { get; set; } = "1.0";
    }

    /// <summary>
    /// Filter criteria for training data
    /// </summary>
    public class TrainingDataFilter
    {
        public DateRange DateRange { get; set; } = DateRange.All;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> AgentTypes { get; set; } = new List<string>();
        public List<string> AgentNames { get; set; } = new List<string>();
        public bool? SuccessOnly { get; set; }
        public TimeSpan? MinProcessingTime { get; set; }
        public TimeSpan? MaxProcessingTime { get; set; }
        public List<string> ExcludeUsers { get; set; } = new List<string>();
    }

    /// <summary>
    /// Export formats for training data
    /// </summary>
    public enum ExportFormat
    {
        Json,
        Csv,
        Xml,
        Parquet,
        Jsonl,
        Avro
    }

    /// <summary>
    /// Date range options for filtering
    /// </summary>
    public enum DateRange
    {
        All,
        Today,
        Yesterday,
        LastWeek,
        LastMonth,
        LastQuarter,
        LastYear,
        Custom
    }

    /// <summary>
    /// Result of training data export operation
    /// </summary>
    public class TrainingDataExportResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public int RecordCount { get; set; }
        public long DataSize { get; set; }
        public double ExportTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Options for data collection
    /// </summary>
    public class DataCollectionOptions
    {
        public bool Enabled { get; set; } = true;
        public double SamplingRate { get; set; } = 1.0;
        public List<string> ExcludedAgents { get; set; } = new List<string>();
        public bool RequireUserConsent { get; set; } = true;
        public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(365);
        public bool AnonymizeImmediately { get; set; } = true;
    }

    /// <summary>
    /// Options for data anonymization
    /// </summary>
    public class DataAnonymizationOptions
    {
        public bool RemovePersonalInfo { get; set; } = true;
        public bool HashUserIds { get; set; } = true;
        public bool RemoveFilePaths { get; set; } = true;
        public bool GeneralizeTimestamps { get; set; } = true;
        public bool RemoveIpAddresses { get; set; } = true;
        public bool AnonymizeCodeContent { get; set; } = false;
        public TrainingDataFilter Filter { get; set; } = new TrainingDataFilter();
    }

    /// <summary>
    /// Statistics about data collection
    /// </summary>
    public class CollectionStatistics
    {
        public int TotalInteractions { get; set; }
        public int UniqueUsers { get; set; }
        public long TotalDataSize { get; set; }
        public double CollectionRate { get; set; }
        public TimeSpan CollectionPeriod { get; set; }
        public DateTime LastCollectionTime { get; set; }
    }

    /// <summary>
    /// Detailed statistics for reporting
    /// </summary>
    public class DetailedStatistics
    {
        public int TotalInteractions { get; set; }
        public int UniqueUsers { get; set; }
        public long TotalDataSize { get; set; }
        public TimeSpan CollectionPeriod { get; set; }
        public Dictionary<string, int> AgentDistribution { get; set; } = new Dictionary<string, int>();
        public double SuccessRate { get; set; }
        public double AverageResponseTime { get; set; }
        public double DataQualityScore { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Anonymization statistics
    /// </summary>
    public class AnonymizationStatistics
    {
        public int TotalProcessed { get; set; }
        public int SuccessfullyAnonymized { get; set; }
        public int PiiInstancesRemoved { get; set; }
        public int UserIdsHashed { get; set; }
        public int FilePathsAnonymized { get; set; }
        public double ProcessingTime { get; set; }
    }

    /// <summary>
    /// Privacy compliance status
    /// </summary>
    public class PrivacyComplianceStatus
    {
        public bool IsCompliant { get; set; }
        public List<string> ComplianceIssues { get; set; } = new List<string>();
        public Dictionary<string, bool> ComplianceChecks { get; set; } = new Dictionary<string, bool>();
        public DateTime LastAuditDate { get; set; }
    }

    /// <summary>
    /// Export validation result
    /// </summary>
    public class ExportValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Training data generator capabilities
    /// </summary>
    public class TrainingDataCapabilities
    {
        public string[] SupportedFormats { get; set; } = Array.Empty<string>();
        public string[] PrivacyFeatures { get; set; } = Array.Empty<string>();
        public string[] ExportOptions { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Types of training data requests
    /// </summary>
    public enum TrainingDataRequestType
    {
        Export,
        Collect,
        Anonymize,
        Statistics,
        General
    }
}