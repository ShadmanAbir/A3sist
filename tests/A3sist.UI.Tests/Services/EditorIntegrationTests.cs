using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;
using A3sist.UI.Editors;
using A3sist.UI.Services;

namespace A3sist.UI.Tests.Services
{
    [TestClass]
    public class EditorIntegrationTests
    {
        private Mock<IServiceProvider> _mockServiceProvider;
        private Mock<ILogger<EditorIntegrationService>> _mockLogger;
        private Mock<ILogger<CodeAnalysisProvider>> _mockCodeAnalysisLogger;
        private Mock<ILogger<SuggestionProvider>> _mockSuggestionLogger;
        private Mock<IOrchestrator> _mockOrchestrator;
        private CodeAnalysisProvider _codeAnalysisProvider;
        private SuggestionProvider _suggestionProvider;
        private EditorIntegrationService _editorIntegrationService;

        [TestInitialize]
        public void Setup()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<EditorIntegrationService>>();
            _mockCodeAnalysisLogger = new Mock<ILogger<CodeAnalysisProvider>>();
            _mockSuggestionLogger = new Mock<ILogger<SuggestionProvider>>();
            _mockOrchestrator = new Mock<IOrchestrator>();

            _codeAnalysisProvider = new CodeAnalysisProvider(_mockOrchestrator.Object, _mockCodeAnalysisLogger.Object);
            _suggestionProvider = new SuggestionProvider(_mockOrchestrator.Object, _codeAnalysisProvider, _mockSuggestionLogger.Object);
            _editorIntegrationService = new EditorIntegrationService(
                _mockServiceProvider.Object,
                _mockLogger.Object,
                _codeAnalysisProvider,
                _suggestionProvider);
        }

        [TestMethod]
        public async Task AnalyzeCodeAsync_WithValidCode_ReturnsAnalysisResult()
        {
            // Arrange
            var filePath = "test.cs";
            var code = @"
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = 42; // Magic number
                        if (x > 0)
                        {
                            Console.WriteLine(""Hello"");
                        }
                    }
                }";

            var expectedResult = new AgentResult
            {
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["CodeAnalysis"] = new CodeAnalysisResult
                    {
                        Language = "C#",
                        Elements = new List<CodeElement>
                        {
                            new CodeElement
                            {
                                Name = "TestClass",
                                Type = CodeElementType.Class,
                                StartLine = 2,
                                EndLine = 10
                            }
                        },
                        Complexity = new ComplexityMetrics
                        {
                            CyclomaticComplexity = 2,
                            LinesOfCode = 9
                        }
                    }
                }
            };

            _mockOrchestrator.Setup(o => o.ProcessRequestAsync(It.IsAny<AgentRequest>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _codeAnalysisProvider.AnalyzeCodeAsync(code, filePath);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("C#", result.Language);
            Assert.IsTrue(result.Elements.Any());
            Assert.IsNotNull(result.Complexity);
            Assert.AreEqual(2, result.Complexity.CyclomaticComplexity);
        }

        [TestMethod]
        public async Task GetSuggestionsAsync_WithCodeSmells_ReturnsSuggestions()
        {
            // Arrange
            var filePath = "test.cs";
            var lineNumber = 5;
            var code = @"
                public class TestClass
                {
                    public void VeryLongMethodNameThatShouldBeRefactored()
                    {
                        var magicNumber = 42;
                        // ... many lines of code ...
                    }
                }";

            var analysisResult = new CodeAnalysisResult
            {
                Language = "C#",
                CodeSmells = new List<CodeSmell>
                {
                    new CodeSmell
                    {
                        Name = "Magic Numbers",
                        Description = "Numeric literals should be replaced with named constants",
                        Type = CodeSmellType.MagicNumbers,
                        Severity = CodeSmellSeverity.Minor,
                        StartLine = 6,
                        EndLine = 6,
                        Suggestion = "Replace magic numbers with named constants"
                    }
                }
            };

            var orchestratorResult = new AgentResult
            {
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["CodeAnalysis"] = analysisResult
                }
            };

            _mockOrchestrator.Setup(o => o.ProcessRequestAsync(It.IsAny<AgentRequest>(), default))
                .ReturnsAsync(orchestratorResult);

            // Act
            var suggestions = await _suggestionProvider.GetSuggestionsAsync(filePath, lineNumber);

            // Assert
            Assert.IsNotNull(suggestions);
            Assert.IsTrue(suggestions.Count > 0);
            
            var magicNumberSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("Magic"));
            Assert.IsNotNull(magicNumberSuggestion);
            Assert.AreEqual(SuggestionType.Naming, magicNumberSuggestion.Type);
        }

        [TestMethod]
        public async Task GetAlternativeSuggestionsAsync_WithOriginalSuggestion_ReturnsAlternatives()
        {
            // Arrange
            var originalSuggestion = new CodeSuggestion
            {
                Id = Guid.NewGuid(),
                FilePath = "test.cs",
                Title = "Extract Method",
                Description = "Extract this code into a separate method",
                Type = SuggestionType.Refactoring,
                StartLine = 5,
                EndLine = 10
            };

            var alternativeSuggestions = new List<CodeSuggestion>
            {
                new CodeSuggestion
                {
                    Title = "Inline Method",
                    Description = "Inline this method call",
                    Type = SuggestionType.Refactoring
                },
                new CodeSuggestion
                {
                    Title = "Rename Method",
                    Description = "Rename method to be more descriptive",
                    Type = SuggestionType.Naming
                }
            };

            var orchestratorResult = new AgentResult
            {
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["AlternativeSuggestions"] = alternativeSuggestions
                }
            };

            _mockOrchestrator.Setup(o => o.ProcessRequestAsync(It.IsAny<AgentRequest>(), default))
                .ReturnsAsync(orchestratorResult);

            // Act
            var alternatives = await _suggestionProvider.GetAlternativeSuggestionsAsync(originalSuggestion);

            // Assert
            Assert.IsNotNull(alternatives);
            Assert.AreEqual(2, alternatives.Count);
            Assert.IsTrue(alternatives.Any(a => a.Title == "Inline Method"));
            Assert.IsTrue(alternatives.Any(a => a.Title == "Rename Method"));
        }

        [TestMethod]
        public async Task GetSuggestionsForLocationAsync_WithSpecificLocation_ReturnsRelevantSuggestions()
        {
            // Arrange
            var filePath = "test.cs";
            var lineNumber = 5;
            var columnNumber = 10;

            var suggestions = new List<CodeSuggestion>
            {
                new CodeSuggestion
                {
                    Title = "Relevant Suggestion",
                    StartLine = 5,
                    StartColumn = 8,
                    Confidence = 0.9
                },
                new CodeSuggestion
                {
                    Title = "Distant Suggestion",
                    StartLine = 20,
                    StartColumn = 1,
                    Confidence = 0.7
                }
            };

            var analysisResult = new CodeAnalysisResult
            {
                Language = "C#",
                CodeSmells = new List<CodeSmell>()
            };

            var orchestratorResult = new AgentResult
            {
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["CodeAnalysis"] = analysisResult
                }
            };

            _mockOrchestrator.Setup(o => o.ProcessRequestAsync(It.IsAny<AgentRequest>(), default))
                .ReturnsAsync(orchestratorResult);

            // Mock the suggestion provider to return our test suggestions
            var mockSuggestionProvider = new Mock<SuggestionProvider>(_mockOrchestrator.Object, _codeAnalysisProvider, _mockSuggestionLogger.Object);
            mockSuggestionProvider.Setup(s => s.GetSuggestionsAsync(filePath, lineNumber))
                .ReturnsAsync(suggestions);

            var editorService = new EditorIntegrationService(
                _mockServiceProvider.Object,
                _mockLogger.Object,
                _codeAnalysisProvider,
                mockSuggestionProvider.Object);

            // Act
            var result = await editorService.GetSuggestionsForLocationAsync(filePath, lineNumber, columnNumber);

            // Assert
            Assert.IsNotNull(result);
            // The relevant suggestion should be included, distant one might be filtered out
            Assert.IsTrue(result.Any(s => s.Title == "Relevant Suggestion"));
        }

        [TestMethod]
        public void CodeAnalysisProvider_DetectBasicCodeSmells_FindsMagicNumbers()
        {
            // Arrange
            var code = @"
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var timeout = 5000; // Magic number
                        var count = 42;     // Another magic number
                        var flag = true;    // Not a magic number
                    }
                }";

            // Act
            var result = _codeAnalysisProvider.AnalyzeCodeAsync(code, "test.cs").Result;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.CodeSmells);
            
            var magicNumberSmells = result.CodeSmells.Where(cs => cs.Type == CodeSmellType.MagicNumbers).ToList();
            Assert.IsTrue(magicNumberSmells.Count > 0, "Should detect magic numbers");
        }

        [TestMethod]
        public void CodeAnalysisProvider_CalculateComplexity_ReturnsValidMetrics()
        {
            // Arrange
            var code = @"
                public class TestClass
                {
                    public void ComplexMethod()
                    {
                        if (condition1)
                        {
                            if (condition2)
                            {
                                while (condition3)
                                {
                                    for (int i = 0; i < 10; i++)
                                    {
                                        // Complex logic
                                    }
                                }
                            }
                        }
                    }
                }";

            // Act
            var result = _codeAnalysisProvider.AnalyzeCodeAsync(code, "test.cs").Result;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Complexity);
            Assert.IsTrue(result.Complexity.CyclomaticComplexity > 1, "Should calculate cyclomatic complexity");
            Assert.IsTrue(result.Complexity.LinesOfCode > 0, "Should count lines of code");
            Assert.IsTrue(result.Complexity.MaintainabilityIndex >= 0 && result.Complexity.MaintainabilityIndex <= 100, 
                "Maintainability index should be between 0 and 100");
        }

        [TestMethod]
        public void CodeAnalysisProvider_ExtractBasicElements_FindsClassesAndMethods()
        {
            // Arrange
            var code = @"
                public class TestClass
                {
                    public void TestMethod()
                    {
                        // Method body
                    }

                    public string GetValue()
                    {
                        return ""test"";
                    }
                }";

            // Act
            var result = _codeAnalysisProvider.AnalyzeCodeAsync(code, "test.cs").Result;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Elements);
            
            var classes = result.Elements.Where(e => e.Type == CodeElementType.Class).ToList();
            var methods = result.Elements.Where(e => e.Type == CodeElementType.Method).ToList();
            
            Assert.IsTrue(classes.Count > 0, "Should find class elements");
            Assert.IsTrue(methods.Count > 0, "Should find method elements");
            
            var testClass = classes.FirstOrDefault(c => c.Name == "TestClass");
            Assert.IsNotNull(testClass, "Should find TestClass");
        }

        [TestMethod]
        public void CodeAnalysisProvider_CacheManagement_WorksCorrectly()
        {
            // Arrange
            var filePath = "test.cs";
            var code = "public class Test { }";

            // Act - First analysis
            var result1 = _codeAnalysisProvider.AnalyzeCodeAsync(code, filePath).Result;
            
            // Act - Second analysis (should use cache)
            var result2 = _codeAnalysisProvider.AnalyzeCodeAsync(code, filePath).Result;

            // Assert
            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.AreEqual(result1.Language, result2.Language);

            // Act - Clear cache and analyze again
            _codeAnalysisProvider.ClearCache(filePath);
            var result3 = _codeAnalysisProvider.AnalyzeCodeAsync(code, filePath).Result;

            // Assert
            Assert.IsNotNull(result3);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _editorIntegrationService?.Dispose();
        }
    }
}