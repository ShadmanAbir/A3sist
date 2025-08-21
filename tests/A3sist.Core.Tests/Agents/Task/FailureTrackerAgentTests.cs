using A3sist.Core.Agents.Task.FailureTracker;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Task
{
    public class FailureTrackerAgentTests : IDisposable
    {
        private readonly Mock<ILogger<FailureTrackerAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly FailureTrackerAgent _failureTrackerAgent;

        public FailureTrackerAgentTests()
        {
            _mockLogger = new Mock<ILogger<FailureTrackerAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();

            // Setup default configuration
            _mockConfiguration.Setup(x => x.GetAgentConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentConfiguration
                {
                    Name = "FailureTracker",
                    Type = AgentType.Fixer,
                    Enabled = true,
                    Settings = new Dictionary<string, object>()
                });

            _failureTrackerAgent = new FailureTrackerAgent(_mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Assert
            Assert.Equal("FailureTracker", _failureTrackerAgent.Name);
            Assert.Equal(AgentType.Fixer, _failureTrackerAgent.Type);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FailureTrackerAgent(null!, _mockConfiguration.Object));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new FailureTrackerAgent(_mockLogger.Object, null!));
        }

        [Theory]
        [InlineData("track this error", true)]
        [InlineData("analyze failure patterns", true)]
        [InlineData("suggest recovery options", true)]
        [InlineData("diagnose the problem", true)]
        [InlineData("investigate this issue", true)]
        [InlineData("troubleshoot the failure", true)]
        [InlineData("fix the error", true)]
        [InlineData("resolve this problem", true)]
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
            var result = await _failureTrackerAgent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CanHandleAsync_WithExceptionInContext_ReturnsTrue()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "some request",
                Context = new Dictionary<string, object>
                {
                    { "exception", "System.NullReferenceException: Object reference not set" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.CanHandleAsync(request);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanHandleAsync_WithErrorInContext_ReturnsTrue()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "some request",
                Context = new Dictionary<string, object>
                {
                    { "error", "Database connection failed" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.CanHandleAsync(request);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _failureTrackerAgent.CanHandleAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HandleAsync_WithTrackFailureRequest_TracksFailureSuccessfully()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "track this error",
                Context = new Dictionary<string, object>
                {
                    { "error", "NullReferenceException: Object reference not set" },
                    { "component", "UserService" },
                    { "stackTrace", "at UserService.GetUser(Int32 id)" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Failure tracked successfully", result.Message);
            Assert.NotNull(result.Content);
            Assert.Contains("NullReference", result.Content); // Should categorize as NullReference
        }

        [Fact]
        public async Task HandleAsync_WithAnalyzePatternRequest_ReturnsPatternAnalysis()
        {
            // Arrange
            // First track some failures to have data to analyze
            await TrackSampleFailures();

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "analyze failure patterns",
                Context = new Dictionary<string, object>
                {
                    { "startDate", DateTime.UtcNow.AddDays(-7).ToString() },
                    { "endDate", DateTime.UtcNow.ToString() }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Pattern analysis completed", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithRecoveryRequest_SuggestsRecoveryOptions()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "suggest recovery for this error",
                Context = new Dictionary<string, object>
                {
                    { "error", "Connection timeout occurred" },
                    { "component", "DatabaseService" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("recovery options", result.Message);
            Assert.NotNull(result.Content);
            Assert.Contains("Retry Operation", result.Content); // Should suggest retry for timeout
        }

        [Fact]
        public async Task HandleAsync_WithDiagnoseRequest_ProvidesDiagnosis()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "diagnose this failure",
                Context = new Dictionary<string, object>
                {
                    { "error", "OutOfMemoryException: Insufficient memory" },
                    { "component", "ImageProcessor" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Diagnosis completed", result.Message);
            Assert.NotNull(result.Content);
            Assert.Contains("Memory", result.Content); // Should categorize as Memory issue
        }

        [Fact]
        public async Task HandleAsync_WithReportRequest_GeneratesFailureReport()
        {
            // Arrange
            // First track some failures to have data to report
            await TrackSampleFailures();

            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "generate failure report",
                Context = new Dictionary<string, object>
                {
                    { "startDate", DateTime.UtcNow.AddDays(-7).ToString() },
                    { "endDate", DateTime.UtcNow.ToString() }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Failure report generated", result.Message);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithGenericFailureRequest_TracksAndSuggestsRecovery()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "something went wrong",
                Context = new Dictionary<string, object>
                {
                    { "error", "Service unavailable" },
                    { "component", "PaymentService" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Failure tracked and recovery suggestions provided", result.Message);
            Assert.NotNull(result.Content);
        }

        [Theory]
        [InlineData("NullReferenceException", "NullReference")]
        [InlineData("Connection timeout", "Timeout")]
        [InlineData("Network error occurred", "Network")]
        [InlineData("OutOfMemoryException", "Memory")]
        [InlineData("Database connection failed", "Database")]
        [InlineData("Access denied", "Security")]
        [InlineData("Configuration missing", "Configuration")]
        [InlineData("File not found", "FileSystem")]
        [InlineData("Unknown error", "Unknown")]
        public async Task HandleAsync_WithDifferentErrorTypes_CategorizesCorrectly(string errorMessage, string expectedCategory)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "track this error",
                Context = new Dictionary<string, object>
                {
                    { "error", errorMessage },
                    { "component", "TestComponent" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains(expectedCategory, result.Content);
        }

        [Theory]
        [InlineData("Critical system failure", FailureSeverity.Critical)]
        [InlineData("Fatal error occurred", FailureSeverity.Critical)]
        [InlineData("Error processing request", FailureSeverity.High)]
        [InlineData("Exception thrown", FailureSeverity.High)]
        [InlineData("Warning: timeout occurred", FailureSeverity.Medium)]
        [InlineData("Info: operation completed", FailureSeverity.Low)]
        public async Task HandleAsync_WithDifferentSeverities_AssignsSeverityCorrectly(string errorMessage, FailureSeverity expectedSeverity)
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "track this error",
                Context = new Dictionary<string, object>
                {
                    { "error", errorMessage },
                    { "component", "TestComponent" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains(expectedSeverity.ToString(), result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithTimeoutError_SuggestsRetryStrategy()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "suggest recovery",
                Context = new Dictionary<string, object>
                {
                    { "error", "Operation timed out" },
                    { "component", "ApiClient" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Retry Operation", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithMemoryError_SuggestsRestartStrategy()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "suggest recovery",
                Context = new Dictionary<string, object>
                {
                    { "error", "OutOfMemoryException" },
                    { "component", "DataProcessor" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Restart Service", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithNetworkError_SuggestsFallbackStrategy()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "suggest recovery",
                Context = new Dictionary<string, object>
                {
                    { "error", "Network connection failed" },
                    { "component", "ExternalService" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Fallback to Alternative", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithUnknownError_SuggestsManualInvestigation()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "suggest recovery",
                Context = new Dictionary<string, object>
                {
                    { "error", "Mysterious logic error" },
                    { "component", "BusinessLogic" }
                }
            };

            // Act
            var result = await _failureTrackerAgent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Manual Investigation", result.Content);
        }

        [Fact]
        public async Task InitializeAsync_CompletesSuccessfully()
        {
            // Act & Assert
            await _failureTrackerAgent.InitializeAsync();
            
            // Verify configuration was called
            _mockConfiguration.Verify(x => x.GetAgentConfigurationAsync("FailureTracker"), Times.Once);
        }

        [Fact]
        public async Task ShutdownAsync_CompletesSuccessfully()
        {
            // Arrange
            await _failureTrackerAgent.InitializeAsync();

            // Act & Assert
            await _failureTrackerAgent.ShutdownAsync();
        }

        [Fact]
        public void Dispose_CompletesWithoutException()
        {
            // Act & Assert
            _failureTrackerAgent.Dispose();
        }

        private async Task TrackSampleFailures()
        {
            var sampleFailures = new[]
            {
                new AgentRequest
                {
                    Id = Guid.NewGuid(),
                    Prompt = "track error",
                    Context = new Dictionary<string, object>
                    {
                        { "error", "NullReferenceException" },
                        { "component", "UserService" }
                    }
                },
                new AgentRequest
                {
                    Id = Guid.NewGuid(),
                    Prompt = "track error",
                    Context = new Dictionary<string, object>
                    {
                        { "error", "Connection timeout" },
                        { "component", "DatabaseService" }
                    }
                },
                new AgentRequest
                {
                    Id = Guid.NewGuid(),
                    Prompt = "track error",
                    Context = new Dictionary<string, object>
                    {
                        { "error", "OutOfMemoryException" },
                        { "component", "ImageProcessor" }
                    }
                }
            };

            foreach (var failure in sampleFailures)
            {
                await _failureTrackerAgent.HandleAsync(failure);
            }
        }

        public void Dispose()
        {
            try
            {
                _failureTrackerAgent?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}