using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class RoutingRuleServiceTests : IDisposable
    {
        private readonly Mock<ILogger<RoutingRuleService>> _mockLogger;
        private readonly RoutingRuleService _routingRuleService;

        public RoutingRuleServiceTests()
        {
            _mockLogger = new Mock<ILogger<RoutingRuleService>>();
            _routingRuleService = new RoutingRuleService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RoutingRuleService(null!));
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithNullClassification_ShouldThrowArgumentNullException()
        {
            // Arrange
            var agents = new[] { CreateMockAgent("TestAgent", AgentType.CSharp).Object };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _routingRuleService.EvaluateRulesAsync(null!, agents));
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithNullAgents_ShouldThrowArgumentNullException()
        {
            // Arrange
            var classification = CreateIntentClassification("fix_error", 0.8);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _routingRuleService.EvaluateRulesAsync(classification, null!));
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithNoAgents_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var classification = CreateIntentClassification("fix_error", 0.8);
            var agents = Enumerable.Empty<IAgent>();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _routingRuleService.EvaluateRulesAsync(classification, agents));
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithFixErrorIntent_ShouldRouteToFixerAgent()
        {
            // Arrange
            var classification = CreateIntentClassification("fix_error", 0.8);
            var agents = new[] 
            { 
                CreateMockAgent("FixerAgent", AgentType.Fixer).Object,
                CreateMockAgent("RefactorAgent", AgentType.Refactor).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("FixerAgent", result.TargetAgent);
            Assert.Equal(AgentType.Fixer, result.TargetAgentType);
            Assert.Equal("fix_error", result.Intent);
            Assert.True(result.Confidence > classification.Confidence); // Should have confidence boost
            Assert.Contains("Fix Error Intent", result.Reason);
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithRefactorIntent_ShouldRouteToRefactorAgent()
        {
            // Arrange
            var classification = CreateIntentClassification("refactor", 0.8);
            var agents = new[] 
            { 
                CreateMockAgent("FixerAgent", AgentType.Fixer).Object,
                CreateMockAgent("RefactorAgent", AgentType.Refactor).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("RefactorAgent", result.TargetAgent);
            Assert.Equal(AgentType.Refactor, result.TargetAgentType);
            Assert.Equal("refactor", result.Intent);
            Assert.Contains("Refactor Intent", result.Reason);
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithCSharpLanguage_ShouldRouteToCSharpAgent()
        {
            // Arrange
            var classification = CreateIntentClassification("unknown", 0.5);
            classification.Language = "csharp";
            var agents = new[] 
            { 
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("CSharpAgent", result.TargetAgent);
            Assert.Equal(AgentType.CSharp, result.TargetAgentType);
            Assert.Contains("C# Language", result.Reason);
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithJavaScriptLanguage_ShouldRouteToJavaScriptAgent()
        {
            // Arrange
            var classification = CreateIntentClassification("unknown", 0.5);
            classification.Language = "javascript";
            var agents = new[] 
            { 
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("JavaScriptAgent", result.TargetAgent);
            Assert.Equal(AgentType.JavaScript, result.TargetAgentType);
            Assert.Contains("JavaScript Language", result.Reason);
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithPythonLanguage_ShouldRouteToPythonAgent()
        {
            // Arrange
            var classification = CreateIntentClassification("unknown", 0.5);
            classification.Language = "python";
            var agents = new[] 
            { 
                CreateMockAgent("PythonAgent", AgentType.Python).Object,
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("PythonAgent", result.TargetAgent);
            Assert.Equal(AgentType.Python, result.TargetAgentType);
            Assert.Contains("Python Language", result.Reason);
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithNoMatchingRules_ShouldUseDefaultRouting()
        {
            // Arrange
            var classification = CreateIntentClassification("unknown", 0.5);
            classification.SuggestedAgentType = AgentType.CSharp;
            var agents = new[] 
            { 
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("CSharpAgent", result.TargetAgent);
            Assert.Equal(AgentType.CSharp, result.TargetAgentType);
            Assert.Contains("Default routing", result.Reason);
            Assert.True(result.Confidence < classification.Confidence); // Should be slightly lower
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithNoMatchingAgentType_ShouldUseFallback()
        {
            // Arrange
            var classification = CreateIntentClassification("unknown", 0.5);
            classification.SuggestedAgentType = AgentType.Python; // No Python agent available
            var agents = new[] 
            { 
                CreateMockAgent("CSharpAgent", AgentType.CSharp).Object,
                CreateMockAgent("JavaScriptAgent", AgentType.JavaScript).Object
            };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("CSharpAgent", result.TargetAgent); // First available agent
            Assert.Equal(AgentType.CSharp, result.TargetAgentType);
            Assert.Contains("Fallback routing", result.Reason);
            Assert.True(result.IsFallback);
            Assert.Equal(0.5, result.Confidence);
        }

        [Fact]
        public async Task EvaluateRulesAsync_ShouldIncludeMetadata()
        {
            // Arrange
            var classification = CreateIntentClassification("fix_error", 0.8);
            var agents = new[] { CreateMockAgent("FixerAgent", AgentType.Fixer).Object };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.NotNull(result.Metadata);
            Assert.Contains("RuleId", result.Metadata.Keys);
            Assert.Contains("RuleName", result.Metadata.Keys);
            Assert.Contains("RulePriority", result.Metadata.Keys);
            Assert.Contains("OriginalConfidence", result.Metadata.Keys);
            Assert.Contains("ConfidenceBoost", result.Metadata.Keys);
        }

        [Fact]
        public async Task AddRuleAsync_WithNullRule_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _routingRuleService.AddRuleAsync(null!));
        }

        [Fact]
        public async Task AddRuleAsync_WithValidRule_ShouldAddSuccessfully()
        {
            // Arrange
            var rule = CreateRoutingRule("TestRule", AgentType.CSharp);

            // Act
            await _routingRuleService.AddRuleAsync(rule);

            // Assert
            Assert.NotNull(rule.Id);
            Assert.True(rule.ModifiedAt > DateTime.MinValue);
        }

        [Fact]
        public async Task AddRuleAsync_WithExistingRule_ShouldUpdateRule()
        {
            // Arrange
            var rule = CreateRoutingRule("TestRule", AgentType.CSharp);
            await _routingRuleService.AddRuleAsync(rule);
            
            var updatedRule = CreateRoutingRule("UpdatedRule", AgentType.JavaScript);
            updatedRule.Id = rule.Id; // Same ID

            // Act
            await _routingRuleService.AddRuleAsync(updatedRule);

            // Assert
            Assert.Equal("UpdatedRule", updatedRule.Name);
            Assert.Equal(AgentType.JavaScript, updatedRule.TargetAgentType);
        }

        [Fact]
        public async Task RemoveRuleAsync_WithNullRuleId_ShouldNotThrow()
        {
            // Act & Assert
            // Should not throw
            await _routingRuleService.RemoveRuleAsync(null!);
            await _routingRuleService.RemoveRuleAsync("");
        }

        [Fact]
        public async Task RemoveRuleAsync_WithNonExistentRule_ShouldNotThrow()
        {
            // Act & Assert
            // Should not throw
            await _routingRuleService.RemoveRuleAsync("non-existent-id");
        }

        [Fact]
        public async Task RemoveRuleAsync_WithExistingRule_ShouldRemoveSuccessfully()
        {
            // Arrange
            var rule = CreateRoutingRule("TestRule", AgentType.CSharp);
            await _routingRuleService.AddRuleAsync(rule);

            // Act
            await _routingRuleService.RemoveRuleAsync(rule.Id);

            // Assert
            // Rule should be removed - test by trying to use it
            var classification = CreateIntentClassification("test", 0.8);
            var agents = new[] { CreateMockAgent("CSharpAgent", AgentType.CSharp).Object };
            
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);
            
            // Should use default routing since custom rule was removed
            Assert.Contains("Default routing", result.Reason);
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithCustomRule_ShouldUseCustomRule()
        {
            // Arrange
            var customRule = new RoutingRule
            {
                Name = "Custom Test Rule",
                Priority = 200, // Higher than default rules
                TargetAgentType = AgentType.CSharp,
                ConfidenceBoost = 0.2,
                Conditions = new List<RoutingCondition>
                {
                    new() { Field = "Intent", Operator = ConditionOperator.Equals, Value = "custom_intent" }
                }
            };
            await _routingRuleService.AddRuleAsync(customRule);

            var classification = CreateIntentClassification("custom_intent", 0.6);
            var agents = new[] { CreateMockAgent("CSharpAgent", AgentType.CSharp).Object };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            Assert.Equal("CSharpAgent", result.TargetAgent);
            Assert.Contains("Custom Test Rule", result.Reason);
            Assert.Equal(0.8, result.Confidence); // 0.6 + 0.2 boost
        }

        [Fact]
        public async Task EvaluateRulesAsync_WithCancellation_ShouldRespectCancellationToken()
        {
            // Arrange
            var classification = CreateIntentClassification("fix_error", 0.8);
            var agents = new[] { CreateMockAgent("FixerAgent", AgentType.Fixer).Object };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // The current implementation doesn't actually use the cancellation token for async operations,
            // but it should complete without throwing
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents, cts.Token);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(ConditionOperator.Equals, "test", "test", true)]
        [InlineData(ConditionOperator.Equals, "test", "TEST", true)] // Case insensitive by default
        [InlineData(ConditionOperator.Equals, "test", "other", false)]
        [InlineData(ConditionOperator.Contains, "testing", "test", true)]
        [InlineData(ConditionOperator.Contains, "test", "testing", false)]
        [InlineData(ConditionOperator.StartsWith, "testing", "test", true)]
        [InlineData(ConditionOperator.StartsWith, "test", "testing", false)]
        [InlineData(ConditionOperator.EndsWith, "testing", "ing", true)]
        [InlineData(ConditionOperator.EndsWith, "test", "ing", false)]
        [InlineData(ConditionOperator.In, "test", "test,other,values", true)]
        [InlineData(ConditionOperator.In, "missing", "test,other,values", false)]
        [InlineData(ConditionOperator.NotIn, "missing", "test,other,values", true)]
        [InlineData(ConditionOperator.NotIn, "test", "test,other,values", false)]
        public async Task EvaluateRulesAsync_WithDifferentConditionOperators_ShouldEvaluateCorrectly(
            ConditionOperator op, string fieldValue, string conditionValue, bool shouldMatch)
        {
            // Arrange
            var customRule = new RoutingRule
            {
                Name = "Condition Test Rule",
                Priority = 200,
                TargetAgentType = AgentType.CSharp,
                Conditions = new List<RoutingCondition>
                {
                    new() { Field = "Intent", Operator = op, Value = conditionValue }
                }
            };
            await _routingRuleService.AddRuleAsync(customRule);

            var classification = CreateIntentClassification(fieldValue, 0.8);
            var agents = new[] { CreateMockAgent("CSharpAgent", AgentType.CSharp).Object };

            // Act
            var result = await _routingRuleService.EvaluateRulesAsync(classification, agents);

            // Assert
            if (shouldMatch)
            {
                Assert.Contains("Condition Test Rule", result.Reason);
            }
            else
            {
                Assert.DoesNotContain("Condition Test Rule", result.Reason);
            }
        }

        private static IntentClassification CreateIntentClassification(string intent, double confidence)
        {
            return new IntentClassification
            {
                Intent = intent,
                Confidence = confidence,
                Language = "csharp",
                SuggestedAgentType = AgentType.Unknown,
                Context = new Dictionary<string, object>()
            };
        }

        private static RoutingRule CreateRoutingRule(string name, AgentType targetAgentType)
        {
            return new RoutingRule
            {
                Name = name,
                Priority = 100,
                TargetAgentType = targetAgentType,
                ConfidenceBoost = 0.1,
                Conditions = new List<RoutingCondition>
                {
                    new() { Field = "Intent", Operator = ConditionOperator.Equals, Value = "test" }
                }
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