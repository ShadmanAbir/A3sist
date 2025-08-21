using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    /// <summary>
    /// Interface for intent classification services
    /// </summary>
    public interface IIntentClassifier
    {
        /// <summary>
        /// Classifies the intent of a request
        /// </summary>
        /// <param name="request">The request to classify</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The classification result</returns>
        Task<IntentClassification> ClassifyAsync(AgentRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Trains the classifier with new data
        /// </summary>
        /// <param name="request">The request used for training</param>
        /// <param name="actualIntent">The actual intent for this request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the training operation</returns>
        Task TrainAsync(AgentRequest request, string actualIntent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the confidence threshold for reliable classifications
        /// </summary>
        double ConfidenceThreshold { get; }
    }

    /// <summary>
    /// Interface for routing rule services
    /// </summary>
    public interface IRoutingRuleService
    {
        /// <summary>
        /// Evaluates routing rules for a classification
        /// </summary>
        /// <param name="classification">The intent classification</param>
        /// <param name="availableAgents">Available agents</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The routing decision</returns>
        Task<RoutingDecision> EvaluateRulesAsync(IntentClassification classification, 
            System.Collections.Generic.IEnumerable<IAgent> availableAgents, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new routing rule
        /// </summary>
        /// <param name="rule">The routing rule to add</param>
        /// <returns>Task representing the add operation</returns>
        Task AddRuleAsync(RoutingRule rule);

        /// <summary>
        /// Removes a routing rule
        /// </summary>
        /// <param name="ruleId">The ID of the rule to remove</param>
        /// <returns>Task representing the remove operation</returns>
        Task RemoveRuleAsync(string ruleId);
    }
}