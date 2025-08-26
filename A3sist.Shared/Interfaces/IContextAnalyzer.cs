using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for analyzing request context to improve knowledge search
    /// </summary>
    public interface IContextAnalyzer
    {
        /// <summary>
        /// Analyze the context of an agent request
        /// </summary>
        Task<KnowledgeContext> AnalyzeContextAsync(
            AgentRequest request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extract programming language from context
        /// </summary>
        string ExtractLanguage(AgentRequest request);

        /// <summary>
        /// Extract project type from context
        /// </summary>
        string ExtractProjectType(AgentRequest request);

        /// <summary>
        /// Determine search scope based on context
        /// </summary>
        SearchScope DetermineSearchScope(AgentRequest request);
    }
}