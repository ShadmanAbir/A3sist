using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using A3sist.Core.Services;
using A3sist.Shared.Models;
using A3sist.TestUtilities;

namespace A3sist.Core.Tests.Services;

/// <summary>
/// Unit tests for LoggingService
/// </summary>
public class LoggingServiceTests : TestBase
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly LoggingService _loggingService;

    public LoggingServiceTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
            
        _loggingService = new LoggingService(_mockLoggerFactory.Object);
    }

    [Fact]
    public void CreateLogger_Generic_ShouldReturnTypedLogger()
    {
        // Arrange
        var mockTypedLogger = new Mock<ILogger<LoggingServiceTests>>();
        _mockLoggerFactory.Setup(x => x.CreateLogger(typeof(LoggingServiceTests).FullName!))
            .Returns(mockTypedLogger.Object);

        // Act
        var logger = _loggingService.CreateLogger<LoggingServiceTests>();

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeSameAs(mockTypedLogger.Object);
        _mockLoggerFactory.Verify(x => x.CreateLogger(typeof(LoggingServiceTests).FullName!), Times.Once);
    }

    [Fact]
    public void CreateLogger_WithCategoryName_ShouldReturnLogger()
    {
        // Arrange
        var categoryName = "TestCategory";

        // Act
        var logger = _loggingService.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeSameAs(_mockLogger.Object);
        _mockLoggerFactory.Verify(x => x.CreateLogger(categoryName), Times.Once);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithValidConfiguration_ShouldUpdateSuccessfully()
    {
        // Arrange
        var configuration = new LoggingConfiguration
        {
            LogLevel = LogLevel.Information,
            OutputPath = @"C:\temp\logs",
            MaxFileSizeMB = 10,
            MaxFileCount = 5,
            EnableConsoleLogging = true,
            EnableFileLogging = true
        };

        // Act
        var act = async () => await _loggingService.UpdateConfigurationAsync(configuration);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void GetConfiguration_ShouldReturnCurrentConfiguration()
    {
        // Act
        var configuration = _loggingService.GetConfiguration();

        // Assert
        configuration.Should().NotBeNull();
        configuration.LogLevel.Should().Be(LogLevel.Information); // Default value
        configuration.EnableConsoleLogging.Should().BeTrue(); // Default value
    }

    [Fact]
    public async Task CleanupLogsAsync_ShouldExecuteWithoutErrors()
    {
        // Act
        var act = async () => await _loggingService.CleanupLogsAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetLogFilesAsync_ShouldReturnLogFileInformation()
    {
        // Act
        var logFiles = await _loggingService.GetLogFilesAsync();

        // Assert
        logFiles.Should().NotBeNull();
        // Note: The actual implementation would return real log file information
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public async Task UpdateConfigurationAsync_WithDifferentLogLevels_ShouldAcceptAllLevels(LogLevel logLevel)
    {
        // Arrange
        var configuration = new LoggingConfiguration
        {
            LogLevel = logLevel
        };

        // Act
        var act = async () => await _loggingService.UpdateConfigurationAsync(configuration);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateConfigurationAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _loggingService.UpdateConfigurationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new LoggingService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task LoggingService_IntegrationTest_ShouldWorkEndToEnd()
    {
        // Arrange
        var configuration = new LoggingConfiguration
        {
            LogLevel = LogLevel.Warning,
            OutputPath = Path.GetTempPath(),
            EnableConsoleLogging = false,
            EnableFileLogging = true
        };

        // Act
        await _loggingService.UpdateConfigurationAsync(configuration);
        var currentConfig = _loggingService.GetConfiguration();
        var logger = _loggingService.CreateLogger("IntegrationTest");
        await _loggingService.CleanupLogsAsync();
        var logFiles = await _loggingService.GetLogFilesAsync();

        // Assert
        currentConfig.Should().NotBeNull();
        logger.Should().NotBeNull();
        logFiles.Should().NotBeNull();
    }
}