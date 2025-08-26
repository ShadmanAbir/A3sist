using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Core.Agents.Core;
using A3sist.Shared.Models;
using A3sist.Shared.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace A3sist.Core.Tests.Agents
{
    /// <summary>
    /// Unit tests for RAGEnhancedCSharpAgent to ensure Roslyn analysis with knowledge augmentation works correctly
    /// </summary>
    public class RAGEnhancedCSharpAgentTests
    {
        private readonly Mock<ILogger<RAGEnhancedCSharpAgent>> _loggerMock;
        private readonly Mock<IRAGService> _ragServiceMock;
        private readonly Mock<ILLMService> _llmServiceMock;
        private readonly RAGEnhancedCSharpAgent _csharpAgent;

        public RAGEnhancedCSharpAgentTests()
        {
            _loggerMock = new Mock<ILogger<RAGEnhancedCSharpAgent>>();
            _ragServiceMock = new Mock<IRAGService>();
            _llmServiceMock = new Mock<ILLMService>();
            
            _csharpAgent = new RAGEnhancedCSharpAgent(
                _loggerMock.Object,
                _ragServiceMock.Object,
                _llmServiceMock.Object);
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithValidCSharpCode_ShouldProvideRoslynAnalysis()
        {
            // Arrange
            var csharpCode = @"
                using System;
                public class TestClass 
                {
                    public void Method()
                    {
                        Console.WriteLine(""Hello World"");
                    }
                }";

            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this C# code",
                Context = new Dictionary<string, object>
                {
                    { "code", csharpCode },
                    { "language", "csharp" }
                }
            };

            var mockKnowledge = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "csharp-best-practices",
                    Title = "C# Best Practices",
                    Content = "Use async/await for I/O operations, follow naming conventions...",
                    Relevance = 0.9f
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockKnowledge);

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Enhanced analysis prompt with C# knowledge");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse 
                { 
                    Success = true, 
                    Content = "Code analysis with Roslyn insights and best practices",
                    Citations = new[] { "C# Best Practices" }
                });

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Content.Should().NotBeNullOrEmpty();
            
            // Verify RAG service was called for C# specific knowledge
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.Is<string>(s => s.Contains("C#") || s.Contains("code analysis")), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithSyntaxErrors_ShouldIdentifyIssues()
        {
            // Arrange
            var invalidCode = @"
                using System;
                public class TestClass 
                {
                    public void Method(
                    {
                        Console.WriteLine(""Missing closing parenthesis"");
                    }
                }"; // Missing closing parenthesis in method signature

            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Check this C# code for issues",
                Context = new Dictionary<string, object>
                {
                    { "code", invalidCode },
                    { "language", "csharp" }
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KnowledgeItem>());

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Syntax error analysis with knowledge");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse 
                { 
                    Success = true, 
                    Content = "Syntax error found: Missing closing parenthesis in method declaration"
                });

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Content.Should().Contain("syntax").Or.Contain("error");
        }

        [Fact]
        public async Task RefactorCodeAsync_WithModernPatterns_ShouldSuggestImprovements()
        {
            // Arrange
            var legacyCode = @"
                public class LegacyService
                {
                    public string GetData()
                    {
                        // Synchronous file operation
                        return System.IO.File.ReadAllText(""data.txt"");
                    }
                }";

            var request = new AgentRequest
            {
                Type = RequestType.Refactoring,
                Content = "Modernize this legacy C# code",
                Context = new Dictionary<string, object>
                {
                    { "code", legacyCode },
                    { "targetFramework", "net9.0" }
                }
            };

            var modernPatternsKnowledge = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "async-patterns",
                    Title = "Modern Async Patterns",
                    Content = "Use async/await for file operations, implement IAsyncDisposable...",
                    Relevance = 0.95f
                },
                new KnowledgeItem
                {
                    Id = "di-patterns",
                    Title = "Dependency Injection Patterns",
                    Content = "Use constructor injection, register services...",
                    Relevance = 0.88f
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(modernPatternsKnowledge);

            _ragServiceMock
                .Setup(r => r.AugmentPromptAsync(It.IsAny<string>(), It.IsAny<IEnumerable<KnowledgeItem>>()))
                .ReturnsAsync("Refactoring prompt with modern patterns knowledge");

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse 
                { 
                    Success = true, 
                    Content = "Refactored code with async patterns and dependency injection",
                    Citations = new[] { "Modern Async Patterns", "Dependency Injection Patterns" }
                });

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Content.Should().Contain("async").Or.Contain("dependency injection");
            result.Citations.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AnalyzeCodeAsync_WithComplexCode_ShouldProvideDetailedAnalysis()
        {
            // Arrange
            var complexCode = @"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Threading.Tasks;

                public class ComplexService
                {
                    private readonly List<string> _data = new List<string>();

                    public async Task<IEnumerable<string>> ProcessDataAsync(IEnumerable<string> input)
                    {
                        return await Task.Run(() =>
                        {
                            return input.Where(x => !string.IsNullOrEmpty(x))
                                      .Select(x => x.ToUpper())
                                      .OrderBy(x => x)
                                      .ToList();
                        });
                    }

                    public void AddData(string item)
                    {
                        _data.Add(item ?? throw new ArgumentNullException(nameof(item)));
                    }
                }";

            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this complex C# service for performance and design issues",
                Context = new Dictionary<string, object>
                {
                    { "code", complexCode },
                    { "analysisType", "comprehensive" }
                }
            };

            var performanceKnowledge = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "performance-patterns",
                    Title = "Performance Analysis Patterns",
                    Content = "Avoid Task.Run for CPU-bound work, use ConfigureAwait(false)...",
                    Relevance = 0.92f
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(performanceKnowledge);

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse 
                { 
                    Success = true, 
                    Content = "Detailed analysis: Task.Run usage, LINQ performance, thread safety concerns"
                });

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Content.Should().NotBeNullOrEmpty();
            
            // Verify comprehensive analysis was requested
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.Is<string>(s => s.Contains("performance") || s.Contains("analysis")), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNonCSharpLanguage_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this Python code",
                Context = new Dictionary<string, object>
                {
                    { "code", "def hello(): print('Hello')" },
                    { "language", "python" }
                }
            };

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("C#");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithMissingCode_ShouldReturnErrorResponse()
        {
            // Arrange
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze code",
                Context = new Dictionary<string, object>
                {
                    { "language", "csharp" }
                    // Missing "code" key
                }
            };

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("code");
        }

        [Theory]
        [InlineData("performance analysis", "performance")]
        [InlineData("security review", "security")]
        [InlineData("refactoring suggestions", "refactoring")]
        [InlineData("code quality check", "quality")]
        public async Task ProcessRequestAsync_WithSpecificAnalysisTypes_ShouldRetrieveRelevantKnowledge(
            string analysisRequest, string expectedKnowledgeArea)
        {
            // Arrange
            var code = "public class TestClass { }";
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = analysisRequest,
                Context = new Dictionary<string, object>
                {
                    { "code", code },
                    { "language", "csharp" }
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KnowledgeItem>());

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse { Success = true, Content = "Analysis complete" });

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            // Verify knowledge retrieval targeted the specific analysis area
            _ragServiceMock.Verify(r => r.RetrieveKnowledgeAsync(
                It.Is<string>(s => s.ToLowerInvariant().Contains(expectedKnowledgeArea)), 
                It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void RAGEnhancedCSharpAgent_Construction_ShouldNotThrow()
        {
            // Arrange & Act
            var action = () => new RAGEnhancedCSharpAgent(
                _loggerMock.Object,
                _ragServiceMock.Object,
                _llmServiceMock.Object);

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public async Task ProcessRequestAsync_WhenRoslynAnalysisFails_ShouldContinueWithLLMAnalysis()
        {
            // Arrange
            var malformedCode = "This is not valid C# code at all!!!";
            var request = new AgentRequest
            {
                Type = RequestType.CodeAnalysis,
                Content = "Analyze this code",
                Context = new Dictionary<string, object>
                {
                    { "code", malformedCode },
                    { "language", "csharp" }
                }
            };

            _ragServiceMock
                .Setup(r => r.RetrieveKnowledgeAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<KnowledgeItem>());

            _llmServiceMock
                .Setup(l => l.ProcessRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentResponse 
                { 
                    Success = true, 
                    Content = "Could not parse as valid C# code, but provided general analysis"
                });

            // Act
            var result = await _csharpAgent.ProcessRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Content.Should().NotBeNullOrEmpty();
        }
    }
}