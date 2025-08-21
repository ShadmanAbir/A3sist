using A3sist.Core.Agents.Core.Designer;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Core
{
    public class DesignerAgentTests : IDisposable
    {
        private readonly Mock<ILogger<DesignerAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly DesignerAgent _designerAgent;
        private readonly string _tempDirectory;

        public DesignerAgentTests()
        {
            _mockLogger = new Mock<ILogger<DesignerAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();

            // Setup default configuration
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentConfiguration
                {
                    Name = "Designer",
                    Type = AgentType.Designer,
                    Enabled = true,
                    Settings = new Dictionary<string, object>()
                });

            _designerAgent = new DesignerAgent(_mockLogger.Object, _mockConfiguration.Object);

            // Create temporary directory for test files
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Assert
            Assert.Equal("Designer", _designerAgent.Name);
            Assert.Equal(AgentType.Designer, _designerAgent.Type);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DesignerAgent(null!, _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DesignerAgent(_mockLogger.Object, null!));
        }

        [Theory]
        [InlineData("analyze the architecture", true)]
        [InlineData("design a new system", true)]
        [InlineData("recommend patterns", true)]
        [InlineData("create scaffolding", true)]
        [InlineData("improve the structure", true)]
        [InlineData("refactor the code", true)]
        [InlineData("plan the architecture", true)]
        [InlineData("hello world", false)]
        [InlineData("", false)]
        public async Task CanHandleAsync_WithVariousPrompts_ReturnsExpectedResult(string prompt, bool expected)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = prompt
            };

            // Act
            var result = await _designerAgent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("test.cs", true)]
        [InlineData("test.js", true)]
        [InlineData("test.py", true)]
        [InlineData("test.java", true)]
        [InlineData("test.txt", false)]
        [InlineData("", false)]
        public async Task CanHandleAsync_WithCodeFiles_ReturnsExpectedResult(string fileName, bool expected)
        {
            // Arrange
            var filePath = string.IsNullOrEmpty(fileName) ? "" : Path.Combine(_tempDirectory, fileName);
            if (!string.IsNullOrEmpty(filePath))
            {
                await File.WriteAllTextAsync(filePath, "// Test content");
            }

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "some request",
                FilePath = filePath
            };

            // Act
            var result = await _designerAgent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _designerAgent.CanHandleAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HandleAsync_WithArchitectureAnalysisRequest_ReturnsAnalysisResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "analyze the architecture of this project",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "TestProject" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Architecture analysis completed", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithDesignPlanRequest_ReturnsDesignPlan()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "create a design plan for a new web application",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "WebApp" },
                    { "language", "C#" },
                    { "framework", "ASP.NET Core" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Design plan created", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithPatternRecommendationRequest_ReturnsPatternRecommendations()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "recommend design patterns for a large application",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "LargeApp" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("pattern recommendations", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithScaffoldingRequest_ReturnsScaffoldingPlan()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "generate scaffolding for a new project",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "NewProject" },
                    { "language", "C#" },
                    { "framework", "ASP.NET Core" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Scaffolding generated", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithImprovementRequest_ReturnsSuggestions()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "suggest improvements for this codebase",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "ExistingProject" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("improvement suggestions", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithComprehensiveAnalysisRequest_ReturnsComprehensiveResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "perform a complete analysis of this project",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "CompleteProject" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Comprehensive analysis completed", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithCodeFileAnalysis_AnalyzesFileStructure()
        {
            // Arrange
            var testFile = Path.Combine(_tempDirectory, "TestClass.cs");
            var largeContent = string.Join("\n", Enumerable.Repeat("// This is a line of code", 500));
            await File.WriteAllTextAsync(testFile, $@"
namespace TestNamespace
{{
    public class TestClass1 {{ }}
    public class TestClass2 {{ }}
    public class TestClass3 {{ }}
    public class TestClass4 {{ }}
    // TODO: Implement this feature
    // FIXME: Fix this bug
}}
{largeContent}");

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "improve this code structure",
                FilePath = testFile,
                Context = new Dictionary<string, object>
                {
                    { "projectName", "TestProject" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("improvement suggestions", result.Message);
            Assert.NotNull(result.Content);
        }

        [Theory]
        [InlineData("C#", "ASP.NET Core")]
        [InlineData("JavaScript", "React")]
        [InlineData("TypeScript", "Angular")]
        [InlineData("Python", "Django")]
        [InlineData("Java", "Spring Boot")]
        public async Task HandleAsync_WithDifferentLanguages_ReturnsAppropriateFramework(string language, string expectedFramework)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "create a design plan",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "TestProject" },
                    { "language", language }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Content);
            // The content should contain the expected framework
            Assert.Contains(expectedFramework, result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithMVCArchitecturePreference_RecommendsMVCPattern()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "design using MVC architecture pattern",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "MVCProject" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("MVC", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithTestabilityPreference_RecommendsTestablePractices()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "design with testability in mind",
                Context = new Dictionary<string, object>
                {
                    { "projectName", "TestableProject" },
                    { "language", "C#" }
                }
            };

            // Act
            var result = await _designerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task InitializeAsync_CompletesSuccessfully()
        {
            // Act & Assert
            await _designerAgent.InitializeAsync();
            
            // Verify configuration was called
            _mockConfiguration.Verify(x => x.GetAgentConfigurationAsync("Designer"), Times.Once);
        }

        [Fact]
        public async Task ShutdownAsync_CompletesSuccessfully()
        {
            // Arrange
            await _designerAgent.InitializeAsync();

            // Act & Assert
            await _designerAgent.ShutdownAsync();
        }

        [Fact]
        public void Dispose_CompletesWithoutException()
        {
            // Act & Assert
            _designerAgent.Dispose();
        }

        public void Dispose()
        {
            try
            {
                _designerAgent?.Dispose();
                
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}