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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Core.Agents.Utility.Knowledge
{
    /// <summary>
    /// Agent responsible for documentation search, retrieval, and knowledge base management
    /// </summary>
    public class KnowledgeAgent : BaseAgent
    {
        private readonly IKnowledgeRepository _knowledgeRepository;
        private readonly IDocumentationSearchService _searchService;
        private readonly IContextAnalyzer _contextAnalyzer;
        private Dictionary<string, KnowledgeEntry> _knowledgeCache;
        private readonly object _cacheLock = new object();

        public override string Name => "KnowledgeAgent";
        public override AgentType Type => AgentType.Utility;

        public KnowledgeAgent(
            ILogger<KnowledgeAgent> logger,
            IAgentConfiguration configuration,
            IKnowledgeRepository knowledgeRepository,
            IDocumentationSearchService searchService,
            IContextAnalyzer contextAnalyzer)
            : base(logger, configuration)
        {
            _knowledgeRepository = knowledgeRepository ?? throw new ArgumentNullException(nameof(knowledgeRepository));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _contextAnalyzer = contextAnalyzer ?? throw new ArgumentNullException(nameof(contextAnalyzer));
            _knowledgeCache = new Dictionary<string, KnowledgeEntry>();
        }

        public override async Task<bool> CanHandleAsync(AgentRequest request)
        {
            if (request?.Prompt == null) return false;

            var prompt = request.Prompt.ToLowerInvariant();
            
            // Knowledge-related keywords
            var knowledgeKeywords = new[]
            {
                "help", "documentation", "docs", "explain", "what is", "how to",
                "example", "tutorial", "guide", "reference", "api", "usage",
                "best practice", "pattern", "convention", "standard"
            };

            return knowledgeKeywords.Any(keyword => prompt.Contains(keyword));
        }

        public override async Task<AgentResult> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.LogInformation("Processing knowledge request: {RequestId}", request.Id);
                
                var context = await _contextAnalyzer.AnalyzeContextAsync(request, cancellationToken);
                var searchResults = await SearchKnowledgeBaseAsync(request.Prompt, context, cancellationToken);
                
                if (!searchResults.Any())
                {
                    // Try broader search or suggest alternatives
                    searchResults = await PerformBroaderSearchAsync(request.Prompt, cancellationToken);
                }

                var response = await GenerateContextualResponseAsync(request, searchResults, context, cancellationToken);
                
                // Update knowledge base with interaction
                await UpdateKnowledgeBaseAsync(request, response, cancellationToken);

                return new AgentResult
                {
                    Success = true,
                    Content = response,
                    Message = "Knowledge retrieved successfully",
                    AgentName = Name,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SearchResultsCount"] = searchResults.Count(),
                        ["Context"] = context.ToString(),
                        ["ResponseType"] = DetermineResponseType(searchResults)
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing knowledge request: {RequestId}", request.Id);
                return new AgentResult
                {
                    Success = false,
                    Message = $"Failed to process knowledge request: {ex.Message}",
                    Exception = ex,
                    AgentName = Name
                };
            }
        }

        private async Task<IEnumerable<KnowledgeEntry>> SearchKnowledgeBaseAsync(
            string query, 
            KnowledgeContext context, 
            CancellationToken cancellationToken)
        {
            // Check cache first
            var cacheKey = GenerateCacheKey(query, context);
            lock (_cacheLock)
            {
                if (_knowledgeCache.TryGetValue(cacheKey, out var cachedEntry) && 
                    !IsEntryExpired(cachedEntry))
                {
                    return new[] { cachedEntry };
                }
            }

            // Search knowledge repository
            var results = await _knowledgeRepository.SearchAsync(query, context, cancellationToken);
            
            // Enhance with external documentation search
            var externalResults = await _searchService.SearchDocumentationAsync(query, context, cancellationToken);
            
            var combinedResults = results.Concat(externalResults).ToList();
            
            // Cache results
            if (combinedResults.Any())
            {
                var topResult = combinedResults.First();
                lock (_cacheLock)
                {
                    _knowledgeCache[cacheKey] = topResult;
                }
            }

            return combinedResults;
        }

        private async Task<IEnumerable<KnowledgeEntry>> PerformBroaderSearchAsync(
            string query, 
            CancellationToken cancellationToken)
        {
            // Extract key terms and search with broader scope
            var keyTerms = ExtractKeyTerms(query);
            var broaderResults = new List<KnowledgeEntry>();

            foreach (var term in keyTerms)
            {
                var results = await _knowledgeRepository.SearchAsync(
                    term, 
                    new KnowledgeContext { Scope = SearchScope.Broad }, 
                    cancellationToken);
                broaderResults.AddRange(results);
            }

            return broaderResults.Distinct().Take(10);
        }

        private async Task<string> GenerateContextualResponseAsync(
            AgentRequest request,
            IEnumerable<KnowledgeEntry> searchResults,
            KnowledgeContext context,
            CancellationToken cancellationToken)
        {
            if (!searchResults.Any())
            {
                return GenerateNoResultsResponse(request.Prompt);
            }

            var response = new List<string>();
            
            // Add direct answer if available
            var directAnswer = searchResults.FirstOrDefault(r => r.Type == KnowledgeEntryType.DirectAnswer);
            if (directAnswer != null)
            {
                response.Add($"**Answer:** {directAnswer.Content}");
            }

            // Add relevant documentation
            var docEntries = searchResults.Where(r => r.Type == KnowledgeEntryType.Documentation).Take(3);
            if (docEntries.Any())
            {
                response.Add("**Documentation:**");
                foreach (var entry in docEntries)
                {
                    response.Add($"- {entry.Title}: {entry.Summary}");
                    if (!string.IsNullOrEmpty(entry.Url))
                    {
                        response.Add($"  Link: {entry.Url}");
                    }
                }
            }

            // Add code examples
            var examples = searchResults.Where(r => r.Type == KnowledgeEntryType.CodeExample).Take(2);
            if (examples.Any())
            {
                response.Add("**Examples:**");
                foreach (var example in examples)
                {
                    response.Add($"```{example.Language ?? "text"}");
                    response.Add(example.Content);
                    response.Add("```");
                }
            }

            // Add related topics
            var relatedTopics = searchResults.Where(r => r.Type == KnowledgeEntryType.RelatedTopic).Take(3);
            if (relatedTopics.Any())
            {
                response.Add("**Related Topics:**");
                response.AddRange(relatedTopics.Select(t => $"- {t.Title}"));
            }

            return string.Join("\n\n", response);
        }

        private async Task UpdateKnowledgeBaseAsync(
            AgentRequest request, 
            string response, 
            CancellationToken cancellationToken)
        {
            try
            {
                var interaction = new KnowledgeInteraction
                {
                    Query = request.Prompt,
                    Response = response,
                    Context = request.Context,
                    Timestamp = DateTime.UtcNow,
                    UserId = request.UserId
                };

                await _knowledgeRepository.RecordInteractionAsync(interaction, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to update knowledge base with interaction");
            }
        }

        private string GenerateNoResultsResponse(string query)
        {
            return $"I couldn't find specific information about '{query}'. " +
                   "You might want to:\n" +
                   "- Check the spelling and try again\n" +
                   "- Use more specific or different keywords\n" +
                   "- Browse the documentation directly\n" +
                   "- Ask a more specific question";
        }

        private string GenerateCacheKey(string query, KnowledgeContext context)
        {
            return $"{query.ToLowerInvariant()}_{context.GetHashCode()}";
        }

        private bool IsEntryExpired(KnowledgeEntry entry)
        {
            return DateTime.UtcNow - entry.LastUpdated > TimeSpan.FromHours(1);
        }

        private IEnumerable<string> ExtractKeyTerms(string query)
        {
            // Simple keyword extraction - could be enhanced with NLP
            var words = Regex.Split(query.ToLowerInvariant(), @"\W+")
                .Where(w => w.Length > 3 && !IsStopWord(w))
                .Distinct()
                .ToList();

            return words;
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>
            {
                "what", "how", "when", "where", "why", "which", "that", "this",
                "with", "from", "they", "them", "their", "there", "then"
            };
            return stopWords.Contains(word);
        }

        private string DetermineResponseType(IEnumerable<KnowledgeEntry> results)
        {
            if (!results.Any()) return "NoResults";
            if (results.Any(r => r.Type == KnowledgeEntryType.DirectAnswer)) return "DirectAnswer";
            if (results.Any(r => r.Type == KnowledgeEntryType.CodeExample)) return "WithExamples";
            return "Documentation";
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Logger.LogInformation("KnowledgeAgent initialized");
            
            // Load initial knowledge base
            await LoadInitialKnowledgeBaseAsync();
        }

        private async Task LoadInitialKnowledgeBaseAsync()
        {
            try
            {
                await _knowledgeRepository.InitializeAsync();
                Logger.LogInformation("Knowledge base initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize knowledge base");
            }
        }

        public override async Task ShutdownAsync()
        {
            Logger.LogInformation("KnowledgeAgent shutting down");
            
            // Clear cache
            lock (_cacheLock)
            {
                _knowledgeCache.Clear();
            }
            
            await base.ShutdownAsync();
        }
    }
}