using A3sist.Core.Agents.Core;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Core
{
    public class IntentRouterAgentTests : IDisposable
    {
        private readonly Mock<IIntentClassifier> _mockIntentClassifier;
        private readonly Mock<IRoutingRuleService> _mockRoutingRuleService;
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly Mock<ILogger<IntentRouterAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly IntentRouterAgent _intentRouter;

        public IntentRouterAgentTests()
        {
            _mockIntentClassifier = new Mock<IIntentClassifier>();
            _mockRoutingRuleService = new Mock<IRoutingRuleService>();
            _mockAgentManager = new Mock<IAgentManager>();
            _mockLogger = new Mock<ILogger<IntentRouterAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();

            _mockIntentClassifier.Setup(x => x.ConfidenceThreshold).Returns(0.7);

            _intentRouter = new IntentRouterAgent(
                _mockIntentClassifier.Object,
                _mockRoutingRuleService.Object,
                _mockAgentManager.Object,
                _mockLogger.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_WithNullIntentClassifier_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new IntentRouterAgent(
                null!,
                _mockRoutingRuleService.Object,
                _mockAgentManager.Object,
                _mockLogger.Object,
                _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullRoutingRuleService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new IntentRouterAgent(
                _mockIntentClassifier.Object,
                null!,
                _mockAgentManager.Object,
                _mockLogger.Object,
                _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullAgentManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new IntentRouterAgent(
                _mockIntentClassifier.Object,
                _mockRoutingRuleService.Object,
                null!,
                _mockLogger.Object,
                _mockConfiguration.Object));
        }

        [Fact]
        public void Properties_ShouldReturnCorrectValues()
        {
            // Assert
            Assert.Equal("IntentRouter", _intentRouter.Name);
            Assert.Equal(AgentType.IntentRouter, _intentRouter.Type);
        }

        [Fact]
        public async Task CanHandleAsync_WithAnyRequest_ShouldReturnTrue()
        {
            // Arrange
            var request = CreateValidRequest();

            // Act
            var result = await _intentRouter.CanHandleAsync(request);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ShouldReturnFalse()
        {
            // Act
            var result = await _intentRouter.CanHandleAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HandleAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _intentRouter.HandleAsync(null!));
        }

        [Fact]
        public async Task HandleAsync_WithValidRequest_ShouldReturnSuccessfulResult()
        {
            // Arrange
            var request = CreateValidRequest();
            var classification = CreateIntentClassification("fix_error", 0.8);
            var routingDecision = CreateRoutingDecision("FixerAgent", AgentType.Fixer, 0.8);
            var availableAgents = new[] { CreateMockAgent("FixerAgent", AgentType.Fixer).Object };

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(classification);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);
            _mockRoutingRuleService.Setup(x => x.EvaluateRulesAsync(classification, availableAgents, It.IsAny<CancellationToken>()))
                .ReturnsAsync(routingDecision);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            Assert.Contains("Classification", result.Metadata.Keys);
            Assert.Contains("RoutingDecision", result.Metadata.Keys);
            Assert.Equal(classification, result.Metadata["Classification"]);
            Assert.Equal(routingDecision, result.Metadata["RoutingDecision"]);
        }

        [Fact]
        public async Task HandleAsync_WithNoAvailableAgents_ShouldReturnFailure()
        {
            // Arrange
            var request = CreateValidRequest();
            var classification = CreateIntentClassification("fix_error", 0.8);

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(classification);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(Enumerable.Empty<IAgent>());

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("No agents available", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithInvalidRoutingDecision_ShouldUseFallback()
        {
            // Arrange
            var request = CreateValidRequest();
            var classification = CreateIntentClassification("fix_error", 0.8);
            var invalidRoutingDecision = CreateRoutingDecision("NonExistentAgent", AgentType.Fixer, 0.8);
            var availableAgents = new[] { CreateMockAgent("FixerAgent", AgentType.Fixer).Object };

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(classification);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);
            _mockRoutingRuleService.Setup(x => x.EvaluateRulesAsync(classification, availableAgents, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidRoutingDecision);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            
            var routingDecision = (RoutingDecision)result.Metadata["RoutingDecision"];
            Assert.Equal("FixerAgent", routingDecision.TargetAgent); // Should use fallback
            Assert.True(routingDecision.IsFallback);
        }

        [Fact]
        public async Task HandleAsync_WithLowConfidenceClassification_ShouldIncludeFollowUpQuestion()
        {
            // Arrange
            var request = CreateValidRequest();
            var classification = CreateIntentClassification("fix_error", 0.5); // Low confidence
            var routingDecision = CreateRoutingDecision("FixerAgent", AgentType.Fixer, 0.5);
            var availableAgents = new[] { CreateMockAgent("FixerAgent", AgentType.Fixer).Object };

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(classification);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);
            _mockRoutingRuleService.Setup(x => x.EvaluateRulesAsync(classification, availableAgents, It.IsAny<CancellationToken>()))
                .ReturnsAsync(routingDecision);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            Assert.Contains("FollowUpQuestion", result.Metadata.Keys);
            Assert.IsType<string>(result.Metadata["FollowUpQuestion"]);
        }

        [Fact]
        public async Task HandleAsync_WithClassificationException_ShouldHandleGracefully()
        {
            // Arrange
            var request = CreateValidRequest();
            var availableAgents = new[] { CreateMockAgent("FixerAgent", AgentType.Fixer).Object };

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Classification failed"));
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success); // Should still succeed with fallback classification
            Assert.NotNull(result.Metadata);
            
            var classification = (IntentClassification)result.Metadata["Classification"];
            Assert.Equal("unknown", classification.Intent);
            Assert.Equal(0.1, classification.Confidence);
        }

        [Fact]
        public async Task HandleAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var request = CreateValidRequest();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var result = await _intentRouter.HandleAsync(request, cts.Token);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Intent routing failed", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithLanguageBasedFallback_ShouldRouteCorrectly()
        {
            // Arrange
            var request = CreateValidRequest();
            request.FilePath = "test.cs"; // C# file
            
            var classification = CreateIntentClassification("unknown", 0.3);
            classification.Language = "csharp";
            
            var invalidRoutingDecision = CreateRoutingDecision("NonExistentAgent", AgentType.Unknown, 0.1);
            var availableAgents = new[] 
            { 
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            _mockIntentClassifier.Setup(x => x.ClassifyAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(classification);
            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);
            _mockRoutingRuleService.Setup(x => x.EvaluateRulesAsync(classification, availableAgents, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidRoutingDecision);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            
            var routingDecision = (RoutingDecision)result.Metadata["RoutingDecision"];
            Assert.Equal("CSharpAgent", routingDecision.TargetAgent);
            Assert.Equal(AgentType.CSharp, routingDecision.TargetAgentType);
            Assert.True(routingDecision.IsFallback);
        }

        private static AgentRequest CreateValidRequest()
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Fix the error in this code",
                FilePath = "test.cs",
                Content = "some code content",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };
        }

        private static IntentClassification CreateIntentClassification(string intent, double confidence)
        {
            return new IntentClassification
            {
                Intent = intent,
                Confidence = confidence,
                Language = "csharp",
                SuggestedAgentType = AgentType.Fixer,
                Keywords = new List<string> { "fix", "error" },
                Context = new Dictionary<string, object>()
            };
        }

        private static RoutingDecision CreateRoutingDecision(string targetAgent, AgentType agentType, double confidence)
        {
            return new RoutingDecision
            {
                TargetAgent = targetAgent,
                TargetAgentType = agentType,
                Intent = "fix_error",
                Confidence = confidence,
                Reason = "Test routing decision"
            };
        }

        private static Mock<IAgent> CreateMockAgent(string name, AgentType type)
        {
            var mockAgent = new Mock<IAgent>();
            mockAgent.Setup(x => x.Name).Returns(name);
            mockAgent.Setup(x => x.Type).Returns(type);
            return mockAgent;
        }

        public void Dispose()
        {
            // No cleanup needed for this test class
        }
    }
}