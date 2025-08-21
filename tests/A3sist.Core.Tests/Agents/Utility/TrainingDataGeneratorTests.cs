using A3sist.Core.Agents.Utility.TrainingData;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Utility
{
    public class TrainingDataGeneratorTests
    {
        private readonly Mock<ILogger<TrainingDataGenerator>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Mock<ITrainingDataRepository> _mockRepository;
        private readonly Mock<IDataAnonymizer> _mockAnonymizer;
        private readonly Mock<ITrainingDataExporter> _mockExporter;
        private readonly Mock<IPrivacyFilter> _mockPrivacyFilter;
        private readonly TrainingDataGenerator _agent;

        public TrainingDataGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<TrainingDataGenerator>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _mockRepository = new Mock<ITrainingDataRepository>();
            _mockAnonymizer = new Mock<IDataAnonymizer>();
            _mockExporter = new Mock<ITrainingDataExporter>();
            _mockPrivacyFilter = new Mock<IPrivacyFilter>();

            _agent = new TrainingDataGenerator(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockRepository.Object,
                _mockAnonymizer.Object,
                _mockExporter.Object,
                _mockPrivacyFilter.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Assert
            Assert.Equal("TrainingDataGenerator", _agent.Name);
            Assert.Equal(AgentType.Utility, _agent.Type);
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TrainingDataGenerator(
                _mockLogger.Object,
                _mockConfiguration.Object,
                null,
                _mockAnonymizer.Object,
                _mockExporter.Object,
                _mockPrivacyFilter.Object));
        }

        [Theory]
        [InlineData("export training data", true)]
        [InlineData("generate dataset", true)]
        [InlineData("collect interactions", true)]
        [InlineData("anonymize data", true)]
        [InlineData("training data statistics", true)]
        [InlineData("usage analytics", true)]
        [InlineData("help me with code", false)]
        [InlineData("refactor this method", false)]
        [InlineData("", false)]
        public async Task CanHandleAsync_WithVariousPrompts_ReturnsExpectedResult(string prompt, bool expected)
        {
            // Arrange
            var request = new AgentRequest { Prompt = prompt };

            // Act
            var result = await _agent.CanHandleAsync(request);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CanHandleAsync_WithNullRequest_ReturnsFalse()
        {
            // Act
            var result = await _agent.CanHandleAsync(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HandleAsync_WithExportRequest_ReturnsExportResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "export training data as JSON"
            };

            var interactions = new List<AgentInteraction>
            {
                new AgentInteraction
                {
                    Id = "test-1",
                    Request = "test request",
                    Response = "test response",
                    Success = true
                }
            };

            var exportResult = new TrainingDataExportResult
            {
                Success = true,
                FilePath = "/path/to/export.json",
                Format = ExportFormat.Json,
                RecordCount = 1,
                DataSize = 1024,
                ExportTime = 1.5
            };

            _mockRepository
                .Setup(x => x.GetInteractionsAsync(It.IsAny<TrainingDataFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(interactions);

            _mockPrivacyFilter
                .Setup(x => x.ShouldIncludeInExportAsync(It.IsAny<AgentInteraction>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockAnonymizer
                .Setup(x => x.AnonymizeInteractionAsync(It.IsAny<AgentInteraction>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AgentInteraction interaction, CancellationToken _) => interaction);

            _mockExporter
                .Setup(x => x.ExportAsync(It.IsAny<IEnumerable<AgentInteraction>>(), It.IsAny<TrainingDataExportOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exportResult);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Training data exported successfully", result.Message);
            Assert.Contains("Export Complete", result.Content);
            Assert.Contains("export.json", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithCollectionRequest_ReturnsCollectionStatus()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "enable data collection"
            };

            var stats = new CollectionStatistics
            {
                TotalInteractions = 1000,
                UniqueUsers = 50,
                CollectionRate = 10.5,
                TotalDataSize = 1024000,
                CollectionPeriod = TimeSpan.FromDays(30)
            };

            _mockRepository
                .Setup(x => x.GetCollectionStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stats);

            _mockRepository
                .Setup(x => x.UpdateCollectionSettingsAsync(It.IsAny<DataCollectionOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Data collection configured successfully", result.Message);
            Assert.Contains("Collection Status", result.Content);
            Assert.Contains("1,000", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithStatisticsRequest_ReturnsDetailedStats()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "show training data statistics"
            };

            var stats = new DetailedStatistics
            {
                TotalInteractions = 5000,
                UniqueUsers = 100,
                TotalDataSize = 5120000,
                CollectionPeriod = TimeSpan.FromDays(90),
                AgentDistribution = new Dictionary<string, int>
                {
                    ["CSharpAgent"] = 2000,
                    ["JavaScriptAgent"] = 1500,
                    ["PythonAgent"] = 1500
                },
                SuccessRate = 95.5,
                AverageResponseTime = 2.3,
                DataQualityScore = 8.7
            };

            _mockRepository
                .Setup(x => x.GetDetailedStatisticsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(stats);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Training data statistics retrieved successfully", result.Message);
            Assert.Contains("Training Data Statistics", result.Content);
            Assert.Contains("5,000", result.Content);
            Assert.Contains("95.5%", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithAnonymizationRequest_ReturnsAnonymizationResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "anonymize training data"
            };

            var interactions = new List<AgentInteraction>
            {
                new AgentInteraction { Id = "1", Request = "test 1" },
                new AgentInteraction { Id = "2", Request = "test 2" }
            };

            _mockRepository
                .Setup(x => x.GetInteractionsAsync(It.IsAny<TrainingDataFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(interactions);

            _mockAnonymizer
                .Setup(x => x.AnonymizeInteractionAsync(It.IsAny<AgentInteraction>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AgentInteraction interaction, CancellationToken _) => interaction);

            _mockRepository
                .Setup(x => x.StoreAnonymizedInteractionsAsync(It.IsAny<IEnumerable<AgentInteraction>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Data anonymization completed successfully", result.Message);
            Assert.Contains("Anonymization Complete", result.Content);
            Assert.Contains("100.0%", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithGeneralRequest_ReturnsCapabilities()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "what can you do with training data"
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Training data generator capabilities", result.Message);
            Assert.Contains("Training Data Generator Capabilities", result.Content);
            Assert.Contains("Data Collection", result.Content);
            Assert.Contains("Export Formats", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "export training data"
            };

            _mockRepository
                .Setup(x => x.GetInteractionsAsync(It.IsAny<TrainingDataFilter>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to process training data request", result.Message);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public async Task RecordInteractionAsync_WithValidInteraction_QueuesForProcessing()
        {
            // Arrange
            var interaction = new AgentInteraction
            {
                Id = "test-interaction",
                Request = "test request",
                Response = "test response"
            };

            _mockPrivacyFilter
                .Setup(x => x.ShouldCollectAsync(interaction, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            await _agent.RecordInteractionAsync(interaction);

            // Assert - No exception should be thrown
            // The interaction should be queued internally
        }

        [Fact]
        public async Task RecordInteractionAsync_WithFilteredInteraction_DoesNotQueue()
        {
            // Arrange
            var interaction = new AgentInteraction
            {
                Id = "filtered-interaction",
                Request = "sensitive request"
            };

            _mockPrivacyFilter
                .Setup(x => x.ShouldCollectAsync(interaction, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _agent.RecordInteractionAsync(interaction);

            // Assert - No exception should be thrown
            // The interaction should not be queued
        }

        [Fact]
        public async Task InitializeAsync_CallsRepositoryInitialize()
        {
            // Arrange
            var collectionOptions = new DataCollectionOptions { Enabled = false };
            
            _mockRepository
                .Setup(x => x.GetCollectionSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(collectionOptions);

            // Act
            await _agent.InitializeAsync();

            // Assert
            _mockRepository.Verify(x => x.InitializeAsync(), Times.Once);
            _mockRepository.Verify(x => x.GetCollectionSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithFailedExport_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "export training data"
            };

            var interactions = new List<AgentInteraction>
            {
                new AgentInteraction { Id = "test" }
            };

            var exportResult = new TrainingDataExportResult
            {
                Success = false,
                ErrorMessage = "Export failed due to disk space"
            };

            _mockRepository
                .Setup(x => x.GetInteractionsAsync(It.IsAny<TrainingDataFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(interactions);

            _mockPrivacyFilter
                .Setup(x => x.ShouldIncludeInExportAsync(It.IsAny<AgentInteraction>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockAnonymizer
                .Setup(x => x.AnonymizeInteractionAsync(It.IsAny<AgentInteraction>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AgentInteraction interaction, CancellationToken _) => interaction);

            _mockExporter
                .Setup(x => x.ExportAsync(It.IsAny<IEnumerable<AgentInteraction>>(), It.IsAny<TrainingDataExportOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exportResult);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Export failed", result.Message);
            Assert.Contains("Export failed due to disk space", result.Content);
        }
    }
}