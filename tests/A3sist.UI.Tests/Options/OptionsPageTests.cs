using A3sist.UI.Options;
using Xunit;

namespace A3sist.UI.Tests.Options;

public class OptionsPageTests
{
    [Fact]
    public void GeneralOptionsPage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var optionsPage = new GeneralOptionsPage();

        // Assert
        Assert.True(optionsPage.EnableA3sist);
        Assert.True(optionsPage.AutoStartOnVSStartup);
        Assert.True(optionsPage.ShowNotifications);
        Assert.False(optionsPage.EnableTelemetry);
        Assert.Equal(5, optionsPage.MaxConcurrentTasks);
        Assert.Equal(300, optionsPage.TaskTimeoutSeconds);
        Assert.True(optionsPage.EnableCaching);
        Assert.Equal(60, optionsPage.CacheExpiryMinutes);
        Assert.Equal(LogLevel.Information, optionsPage.LogLevel);
        Assert.True(optionsPage.EnableFileLogging);
        Assert.Equal("%TEMP%\\A3sist\\logs", optionsPage.LogFilePath);
        Assert.Equal(10, optionsPage.MaxLogFileSizeMB);
        Assert.Equal(10, optionsPage.MaxLogFiles);
    }

    [Fact]
    public void GeneralOptionsPage_ValidateSettings_WithValidValues_ReturnsTrue()
    {
        // Arrange
        var optionsPage = new GeneralOptionsPage
        {
            MaxConcurrentTasks = 5,
            TaskTimeoutSeconds = 300,
            CacheExpiryMinutes = 60,
            MaxLogFileSizeMB = 10,
            MaxLogFiles = 10
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0, 300, 60, 10, 10)] // MaxConcurrentTasks too low
    [InlineData(101, 300, 60, 10, 10)] // MaxConcurrentTasks too high
    [InlineData(5, 5, 60, 10, 10)] // TaskTimeoutSeconds too low
    [InlineData(5, 3601, 60, 10, 10)] // TaskTimeoutSeconds too high
    [InlineData(5, 300, 0, 10, 10)] // CacheExpiryMinutes too low
    [InlineData(5, 300, 1441, 10, 10)] // CacheExpiryMinutes too high
    [InlineData(5, 300, 60, 0, 10)] // MaxLogFileSizeMB too low
    [InlineData(5, 300, 60, 1001, 10)] // MaxLogFileSizeMB too high
    [InlineData(5, 300, 60, 10, 0)] // MaxLogFiles too low
    [InlineData(5, 300, 60, 10, 101)] // MaxLogFiles too high
    public void GeneralOptionsPage_ValidateSettings_WithInvalidValues_ReturnsFalse(
        int maxConcurrentTasks, int taskTimeoutSeconds, int cacheExpiryMinutes, 
        int maxLogFileSizeMB, int maxLogFiles)
    {
        // Arrange
        var optionsPage = new GeneralOptionsPage
        {
            MaxConcurrentTasks = maxConcurrentTasks,
            TaskTimeoutSeconds = taskTimeoutSeconds,
            CacheExpiryMinutes = cacheExpiryMinutes,
            MaxLogFileSizeMB = maxLogFileSizeMB,
            MaxLogFiles = maxLogFiles
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GeneralOptionsPage_ResetToDefaults_RestoresDefaultValues()
    {
        // Arrange
        var optionsPage = new GeneralOptionsPage
        {
            EnableA3sist = false,
            MaxConcurrentTasks = 10,
            LogLevel = LogLevel.Debug
        };

        // Act
        optionsPage.ResetToDefaults();

        // Assert
        Assert.True(optionsPage.EnableA3sist);
        Assert.Equal(5, optionsPage.MaxConcurrentTasks);
        Assert.Equal(LogLevel.Information, optionsPage.LogLevel);
    }

    [Fact]
    public void AgentOptionsPage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var optionsPage = new AgentOptionsPage();

        // Assert
        Assert.True(optionsPage.EnableOrchestrator);
        Assert.Equal(5, optionsPage.OrchestratorMaxConcurrentTasks);
        Assert.Equal(300, optionsPage.OrchestratorTimeoutSeconds);
        Assert.True(optionsPage.EnableCSharpAgent);
        Assert.Equal("Full", optionsPage.CSharpAnalysisLevel);
        Assert.True(optionsPage.EnableJavaScriptAgent);
        Assert.True(optionsPage.EnablePythonAgent);
        Assert.True(optionsPage.EnableFixerAgent);
        Assert.True(optionsPage.EnableRefactorAgent);
        Assert.True(optionsPage.EnableValidatorAgent);
        Assert.True(optionsPage.EnableKnowledgeAgent);
        Assert.False(optionsPage.EnableShellAgent);
        Assert.False(optionsPage.EnableTrainingDataGenerator);
        Assert.True(optionsPage.AutoRetryFailedTasks);
        Assert.Equal(3, optionsPage.MaxRetryAttempts);
        Assert.Equal(5, optionsPage.RetryDelaySeconds);
        Assert.True(optionsPage.EnableAgentHealthMonitoring);
        Assert.Equal(60, optionsPage.HealthCheckIntervalSeconds);
    }

    [Fact]
    public void AgentOptionsPage_ValidateSettings_WithValidValues_ReturnsTrue()
    {
        // Arrange
        var optionsPage = new AgentOptionsPage
        {
            OrchestratorMaxConcurrentTasks = 5,
            OrchestratorTimeoutSeconds = 300,
            CSharpAnalysisLevel = "Full",
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 5,
            HealthCheckIntervalSeconds = 60
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("Basic")]
    [InlineData("Full")]
    [InlineData("Deep")]
    public void AgentOptionsPage_ValidateSettings_WithValidAnalysisLevel_ReturnsTrue(string analysisLevel)
    {
        // Arrange
        var optionsPage = new AgentOptionsPage
        {
            CSharpAnalysisLevel = analysisLevel
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void AgentOptionsPage_ValidateSettings_WithInvalidAnalysisLevel_ReturnsFalse(string analysisLevel)
    {
        // Arrange
        var optionsPage = new AgentOptionsPage
        {
            CSharpAnalysisLevel = analysisLevel
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LLMOptionsPage_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var optionsPage = new LLMOptionsPage();

        // Assert
        Assert.Equal("OpenAI", optionsPage.Provider);
        Assert.Equal("gpt-4", optionsPage.Model);
        Assert.Equal("", optionsPage.ApiKey);
        Assert.Equal("", optionsPage.ApiEndpoint);
        Assert.Equal("", optionsPage.OrganizationId);
        Assert.Equal(4000, optionsPage.MaxTokens);
        Assert.Equal(0.7, optionsPage.Temperature);
        Assert.Equal(1.0, optionsPage.TopP);
        Assert.Equal(0.0, optionsPage.FrequencyPenalty);
        Assert.Equal(0.0, optionsPage.PresencePenalty);
        Assert.Equal(60, optionsPage.RequestTimeoutSeconds);
        Assert.Equal(3, optionsPage.MaxRetryAttempts);
        Assert.Equal(2, optionsPage.RetryDelaySeconds);
        Assert.Equal(30, optionsPage.MaxRetryDelaySeconds);
        Assert.True(optionsPage.EnableRateLimiting);
        Assert.Equal(60, optionsPage.RequestsPerMinute);
        Assert.Equal(90000, optionsPage.TokensPerMinute);
        Assert.True(optionsPage.EnableResponseCaching);
        Assert.Equal(60, optionsPage.CacheTTLMinutes);
        Assert.Equal(100, optionsPage.MaxCacheSizeMB);
        Assert.True(optionsPage.EnableDataAnonymization);
        Assert.False(optionsPage.LogRequests);
        Assert.False(optionsPage.LogResponses);
    }

    [Fact]
    public void LLMOptionsPage_ValidateSettings_WithValidValues_ReturnsTrue()
    {
        // Arrange
        var optionsPage = new LLMOptionsPage
        {
            Provider = "OpenAI",
            Model = "gpt-4",
            MaxTokens = 4000,
            Temperature = 0.7,
            TopP = 1.0,
            FrequencyPenalty = 0.0,
            PresencePenalty = 0.0,
            RequestTimeoutSeconds = 60,
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 2,
            MaxRetryDelaySeconds = 30,
            RequestsPerMinute = 60,
            TokensPerMinute = 90000,
            CacheTTLMinutes = 60,
            MaxCacheSizeMB = 100
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("", "gpt-4")] // Empty provider
    [InlineData("OpenAI", "")] // Empty model
    public void LLMOptionsPage_ValidateSettings_WithEmptyProviderOrModel_ReturnsFalse(string provider, string model)
    {
        // Arrange
        var optionsPage = new LLMOptionsPage
        {
            Provider = provider,
            Model = model
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)] // Too low
    [InlineData(32001)] // Too high
    public void LLMOptionsPage_ValidateSettings_WithInvalidMaxTokens_ReturnsFalse(int maxTokens)
    {
        // Arrange
        var optionsPage = new LLMOptionsPage
        {
            Provider = "OpenAI",
            Model = "gpt-4",
            MaxTokens = maxTokens
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(-0.1)] // Too low
    [InlineData(2.1)] // Too high
    public void LLMOptionsPage_ValidateSettings_WithInvalidTemperature_ReturnsFalse(double temperature)
    {
        // Arrange
        var optionsPage = new LLMOptionsPage
        {
            Provider = "OpenAI",
            Model = "gpt-4",
            Temperature = temperature
        };

        // Act
        var result = optionsPage.ValidateSettings();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LLMOptionsPage_ResetToDefaults_RestoresDefaultValues()
    {
        // Arrange
        var optionsPage = new LLMOptionsPage
        {
            Provider = "Custom",
            Model = "custom-model",
            MaxTokens = 8000,
            Temperature = 1.5
        };

        // Act
        optionsPage.ResetToDefaults();

        // Assert
        Assert.Equal("OpenAI", optionsPage.Provider);
        Assert.Equal("gpt-4", optionsPage.Model);
        Assert.Equal(4000, optionsPage.MaxTokens);
        Assert.Equal(0.7, optionsPage.Temperature);
    }

    [Fact]
    public void BaseOptionsPage_Properties_AreCorrect()
    {
        // Arrange & Act
        var generalPage = new GeneralOptionsPage();
        var agentPage = new AgentOptionsPage();
        var llmPage = new LLMOptionsPage();

        // Assert
        Assert.Equal("A3sist", generalPage.CategoryName);
        Assert.Equal("General", generalPage.PageName);
        
        Assert.Equal("A3sist", agentPage.CategoryName);
        Assert.Equal("Agents", agentPage.PageName);
        
        Assert.Equal("A3sist", llmPage.CategoryName);
        Assert.Equal("LLM", llmPage.PageName);
    }
}