using A3sist.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace A3sist.Core.Tests.Configuration
{
    public class LoggingConfigurationProviderTests
    {
        [Fact]
        public void LoadConfiguration_WithEmptyConfiguration_ShouldReturnDefaults()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var provider = new LoggingConfigurationProvider(configuration);

            // Act
            var config = provider.LoadConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(LogLevel.Information, config.MinimumLevel);
            Assert.True(config.WriteToConsole);
            Assert.True(config.WriteToFile);
            Assert.NotEmpty(config.LogFilePath);
        }

        [Fact]
        public void LoadConfiguration_WithConfigurationSection_ShouldLoadValues()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["A3sist:Logging:MinimumLevel"] = "Debug",
                ["A3sist:Logging:LogFilePath"] = "/custom/path",
                ["A3sist:Logging:MaxFileSizeMB"] = "20",
                ["A3sist:Logging:RetainedFileCountLimit"] = "15",
                ["A3sist:Logging:WriteToConsole"] = "false",
                ["A3sist:Logging:WriteToFile"] = "true"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var provider = new LoggingConfigurationProvider(configuration);

            // Act
            var config = provider.LoadConfiguration();

            // Assert
            Assert.Equal(LogLevel.Debug, config.MinimumLevel);
            Assert.Equal("/custom/path", config.LogFilePath);
            Assert.Equal(20, config.MaxFileSizeMB);
            Assert.Equal(15, config.RetainedFileCountLimit);
            Assert.False(config.WriteToConsole);
            Assert.True(config.WriteToFile);
        }

        [Fact]
        public void LoadConfiguration_WithEnvironmentVariables_ShouldOverrideConfig()
        {
            // Arrange
            var originalLogLevel = Environment.GetEnvironmentVariable("A3SIST_LOG_LEVEL");
            var originalLogPath = Environment.GetEnvironmentVariable("A3SIST_LOG_PATH");

            try
            {
                Environment.SetEnvironmentVariable("A3SIST_LOG_LEVEL", "Warning");
                Environment.SetEnvironmentVariable("A3SIST_LOG_PATH", "/env/path");

                var configuration = new ConfigurationBuilder().Build();
                var provider = new LoggingConfigurationProvider(configuration);

                // Act
                var config = provider.LoadConfiguration();

                // Assert
                Assert.Equal(LogLevel.Warning, config.MinimumLevel);
                Assert.Equal("/env/path", config.LogFilePath);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("A3SIST_LOG_LEVEL", originalLogLevel);
                Environment.SetEnvironmentVariable("A3SIST_LOG_PATH", originalLogPath);
            }
        }

        [Fact]
        public void LoadConfiguration_WithInvalidEnvironmentVariables_ShouldIgnoreInvalidValues()
        {
            // Arrange
            var originalLogLevel = Environment.GetEnvironmentVariable("A3SIST_LOG_LEVEL");
            var originalMaxFileSize = Environment.GetEnvironmentVariable("A3SIST_LOG_MAX_FILE_SIZE_MB");

            try
            {
                Environment.SetEnvironmentVariable("A3SIST_LOG_LEVEL", "InvalidLevel");
                Environment.SetEnvironmentVariable("A3SIST_LOG_MAX_FILE_SIZE_MB", "NotANumber");

                var configuration = new ConfigurationBuilder().Build();
                var provider = new LoggingConfigurationProvider(configuration);

                // Act
                var config = provider.LoadConfiguration();

                // Assert
                Assert.Equal(LogLevel.Information, config.MinimumLevel); // Should use default
                Assert.Equal(10, config.MaxFileSizeMB); // Should use default
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("A3SIST_LOG_LEVEL", originalLogLevel);
                Environment.SetEnvironmentVariable("A3SIST_LOG_MAX_FILE_SIZE_MB", originalMaxFileSize);
            }
        }

        [Fact]
        public void CreateDefault_ShouldReturnValidConfiguration()
        {
            // Act
            var config = LoggingConfigurationProvider.CreateDefault();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(LogLevel.Information, config.MinimumLevel);
            Assert.True(config.WriteToConsole);
            Assert.True(config.WriteToFile);
            Assert.NotEmpty(config.LogFilePath);
            Assert.True(config.MaxFileSizeMB > 0);
            Assert.True(config.RetainedFileCountLimit > 0);
            Assert.NotEmpty(config.OutputTemplate);
            Assert.NotNull(config.LogLevelOverrides);
            Assert.NotNull(config.GlobalProperties);
        }

        [Fact]
        public void LoadConfiguration_ShouldValidateAndApplyLimits()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                ["A3sist:Logging:MaxFileSizeMB"] = "200", // Above limit
                ["A3sist:Logging:RetainedFileCountLimit"] = "150", // Above limit
                ["A3sist:Logging:LogFilePath"] = "", // Empty path
                ["A3sist:Logging:OutputTemplate"] = "" // Empty template
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var provider = new LoggingConfigurationProvider(configuration);

            // Act
            var config = provider.LoadConfiguration();

            // Assert
            Assert.Equal(100, config.MaxFileSizeMB); // Should be capped
            Assert.Equal(100, config.RetainedFileCountLimit); // Should be capped
            Assert.NotEmpty(config.LogFilePath); // Should have default
            Assert.NotEmpty(config.OutputTemplate); // Should have default
        }

        [Fact]
        public void LoadConfiguration_ShouldAddDefaultGlobalProperties()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().Build();
            var provider = new LoggingConfigurationProvider(configuration);

            // Act
            var config = provider.LoadConfiguration();

            // Assert
            Assert.Contains("Application", config.GlobalProperties.Keys);
            Assert.Contains("Version", config.GlobalProperties.Keys);
            Assert.Equal("A3sist", config.GlobalProperties["Application"]);
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LoggingConfigurationProvider(null!));
        }
    }
}