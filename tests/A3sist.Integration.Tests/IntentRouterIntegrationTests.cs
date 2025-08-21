using A3sist.Core.Agents.Core;
using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Integration.Tests
{
    /// <summary>
    /// Integration tests for the IntentRouter system
    /// </summary>
    public class IntentRouterIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<IntentRouterAgent>> _mockRouterLogger;
        private readonly Mock<ILogger<IntentClassifier>> _mockClassifierLogger;
        private readonly Mock<ILogger<RoutingRuleService>> _mockRuleServiceLogger;
        private readonly Mock<IAgentManager> _mockAgentManager;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        
        private readonly IntentClassifier _intentClassifier;
        private readonly RoutingRuleService _routingRuleService;
        private readonly IntentRouterAgent _intentRouter;

        public IntentRouterIntegrationTests()
        {
            _mockRouterLogger = new Mock<ILogger<IntentRouterAgent>>();
            _mockClassifierLogger = new Mock<ILogger<IntentClassifier>>();
            _mockRuleServiceLogger = new Mock<ILogger<RoutingRuleService>>();
            _mockAgentManager = new Mock<IAgentManager>();
            _mockConfiguration = new Mock<IAgentConfiguration>();

            // Create real instances of the services
            _intentClassifier = new IntentClassifier(_mockClassifierLogger.Object);
            _routingRuleService = new RoutingRuleService(_mockRuleServiceLogger.Object);
            
            // Create the IntentRouter with real dependencies
            _intentRouter = new IntentRouterAgent(
                _intentClassifier,
                _routingRuleService,
                _mockAgentManager.Object,
                _mockRouterLogger.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public async Task EndToEndIntentRouting_WithFixErrorRequest_ShouldRouteToFixerAgent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Fix the error in this C# code",
                FilePath = "Program.cs",
                Content = "using System; class Program { static void Main() { Console.WriteLine(\"Hello\"); } }",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            var availableAgents = new[]
            {
                CreateMockAgent("FixerAgent", AgentType.Fixer).Object,
                CreateMockAgent("RefactorAgent", AgentType.Refactor).Object,
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object
            };

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            
            var classification = (IntentClassification)result.Metadata["Classification"];
            var routingDecision = (RoutingDecision)result.Metadata["RoutingDecision"];

            Assert.Equal("fix_error", classification.Intent);
            Assert.Equal("csharp", classification.Language);
            Assert.Equal("FixerAgent", routingDecision.TargetAgent);
            Assert.Equal(AgentType.Fixer, routingDecision.TargetAgentType);
            Assert.True(routingDecision.Confidence > 0.7); // Should have high confidence
        }

        [Fact]
        public async Task EndToEndIntentRouting_WithRefactorRequest_ShouldRouteToRefactorAgent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Refactor this JavaScript function to improve performance",
                FilePath = "script.js",
                Content = "function test() { return 'hello'; }",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            var availableAgents = new[]
            {
                CreateMockAgent("FixerAgent", AgentType.Fixer).Object,
                CreateMockAgent("RefactorAgent", AgentType.Refactor).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            
            var classification = (IntentClassification)result.Metadata["Classification"];
            var routingDecision = (RoutingDecision)result.Metadata["RoutingDecision"];

            Assert.Equal("refactor", classification.Intent);
            Assert.Equal("javascript", classification.Language);
            Assert.Equal("RefactorAgent", routingDecision.TargetAgent);
            Assert.Equal(AgentType.Refactor, routingDecision.TargetAgentType);
        }

        [Fact]
        public async Task EndToEndIntentRouting_WithLanguageOnlyRequest_ShouldRouteByLanguage()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Help me with this Python code",
                FilePath = "main.py",
                Content = "def hello(): print('Hello, World!')",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            var availableAgents = new[]
            {
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("PythonAgent", AgentType.Python).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            
            var classification = (IntentClassification)result.Metadata["Classification"];
            var routingDecision = (RoutingDecision)result.Metadata["RoutingDecision"];

            Assert.Equal("python", classification.Language);
            Assert.Equal("PythonAgent", routingDecision.TargetAgent);
            Assert.Equal(AgentType.Python, routingDecision.TargetAgentType);
        }

        [Fact]
        public async Task EndToEndIntentRouting_WithUnknownIntent_ShouldUseFallbackRouting()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Some random text that doesn't match any patterns",
                FilePath = null,
                Content = null,
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            var availableAgents = new[]
            {
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            _mockAgentManager.Setup(x => x.GetAgentsAsync(It.IsAny<Func<IAgent, bool>>()))
                .ReturnsAsync(availableAgents);

            // Act
            var result = await _intentRouter.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Metadata);
            
            var classification = (IntentClassification)result.Metadata["Classification"];
            var routingDecision = (RoutingDecision)result.Metadata["RoutingDecision"];

            Assert.Equal("unknown", classification.Intent);
            Assert.True(classification.Confidence < 0.5); // Low confidence for unknown
            Assert.NotNull(routingDecision.TargetAgent); // Should still route somewhere
            Assert.True(routingDecision.IsFallback || routingDecision.Reason.Contains("Fallback"));
        }

        [Fact]
        public async Task EndToEndIntentRouting_WithCustomRule_ShouldUseCustomRule()
        {
            // Arrange
            var customRule = new RoutingRule
            {
                Name = "Custom Integration Test Rule",
                Priority = 200, // Higher than default rules
                TargetAgentType = AgentType.CSharp,
                ConfidenceBoost = 0.3,
                Conditions = new List<RoutingCondition>
                {
                    new() { Field = "Intent", Operator = ConditionOperator.Equals, Value = "custom_test_intent" }
                }
            };
            await _routingRuleService.AddRuleAsync(customRule);

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "This should match the custom rule",
                FilePath = "test.cs",
                Content = "// test code",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            // Manually set the intent to match our custom rule
            var classification = new IntentClassification
            {
                Intent = "custom_test_intent",
                Confidence = 0.6,
                Language = "csharp",
                SuggestedAgentType = AgentType.Unknown
            };

            var availableAgents = new[]
            {
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            // Act
            var routingDecision = await _routingRuleService.EvaluateRulesAsync(classification, availableAgents);

            // Assert
            Assert.Equal("CSharpAgent", routingDecision.TargetAgent);
            Assert.Equal(AgentType.CSharp, routingDecision.TargetAgentType);
            Assert.Contains("Custom Integration Test Rule", routingDecision.Reason);
            Assert.Equal(0.9, routingDecision.Confidence); // 0.6 + 0.3 boost
            Assert.NotNull(routingDecision.Metadata);
            Assert.Contains("RuleName", routingDecision.Metadata.Keys);
        }

        [Fact]
        public async Task IntentClassifier_WithMultipleKeywords_ShouldClassifyCorrectly()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Fix the bug and refactor the broken code to improve performance",
                FilePath = "Program.cs",
                Content = "using System;",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };

            // Act
            var classification = await _intentClassifier.ClassifyAsync(request);

            // Assert
            // Should classify as fix_error since it has higher priority keywords
            Assert.True(classification.Intent == "fix_error" || classification.Intent == "refactor");
            Assert.True(classification.Confidence > 0.5);
            Assert.Equal("csharp", classification.Language);
            Assert.NotEmpty(classification.Keywords);
            Assert.NotEmpty(classification.Alternatives); // Should have alternatives due to multiple matches
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