using System;
using System.Collections.Generic;

namespace A3sist.Shared.Models
{
    /// <summary>
    /// Represents a knowledge entry in the knowledge base
    /// </summary>
    public class KnowledgeEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public KnowledgeEntryType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public double Relevance { get; set; }
        public int UsageCount { get; set; }
        public List<string> RelatedEntries { get; set; } = new List<string>();
    }

    /// <summary>
    /// Types of knowledge entries
    /// </summary>
    public enum KnowledgeEntryType
    {
        Documentation,
        CodeExample,
        Tutorial,
        Reference,
        BestPractice,
        Troubleshooting,
        FAQ,
        DirectAnswer,
        RelatedTopic
    }

    /// <summary>
    /// Context information for knowledge searches
    /// </summary>
    public class KnowledgeContext
    {
        public string Language { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public SearchScope Scope { get; set; } = SearchScope.Normal;
        public List<string> Keywords { get; set; } = new List<string>();
        public Dictionary<string, object> AdditionalContext { get; set; } = new Dictionary<string, object>();

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Language?.GetHashCode() ?? 0);
                hash = hash * 23 + (ProjectType?.GetHashCode() ?? 0);
                hash = hash * 23 + (Framework?.GetHashCode() ?? 0);
                hash = hash * 23 + Scope.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>
    /// Search scope for knowledge queries
    /// </summary>
    public enum SearchScope
    {
        Narrow,
        Normal,
        Broad,
        Global
    }

    /// <summary>
    /// Represents a knowledge interaction for learning purposes
    /// </summary>
    public class KnowledgeInteraction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
        public bool WasHelpful { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    /// <summary>
    /// Knowledge search result with ranking information
    /// </summary>
    public class KnowledgeSearchResult
    {
        public KnowledgeEntry Entry { get; set; } = new KnowledgeEntry();
        public double Score { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public List<string> MatchedTerms { get; set; } = new List<string>();
    }
}