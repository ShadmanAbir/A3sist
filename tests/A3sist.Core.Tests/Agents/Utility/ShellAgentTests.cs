using A3sist.Core.Agents.Utility.Shell;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Utility
{
    public class ShellAgentTests
    {
        private readonly Mock<ILogger<ShellAgent>> _mockLogger;
        private readonly Mock<IAgentConfiguration> _mockConfiguration;
        private readonly Mock<ICommandValidator> _mockValidator;
        private readonly Mock<ICommandSandbox> _mockSandbox;
        private readonly Mock<IShellConfiguration> _mockShellConfig;
        private readonly ShellAgent _agent;

        public ShellAgentTests()
        {
            _mockLogger = new Mock<ILogger<ShellAgent>>();
            _mockConfiguration = new Mock<IAgentConfiguration>();
            _mockValidator = new Mock<ICommandValidator>();
            _mockSandbox = new Mock<ICommandSandbox>();
            _mockShellConfig = new Mock<IShellConfiguration>();

            _mockShellConfig.Setup(x => x.CommandTimeout).Returns(TimeSpan.FromMinutes(5));

            _agent = new ShellAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockValidator.Object,
                _mockSandbox.Object,
                _mockShellConfig.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Assert
            Assert.Equal("ShellAgent", _agent.Name);
            Assert.Equal(AgentType.Utility, _agent.Type);
        }

        [Fact]
        public void Constructor_WithNullValidator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ShellAgent(
                _mockLogger.Object,
                _mockConfiguration.Object,
                null,
                _mockSandbox.Object,
                _mockShellConfig.Object));
        }

        [Theory]
        [InlineData("run dotnet build", true)]
        [InlineData("execute npm install", true)]
        [InlineData("command: git status", true)]
        [InlineData("build the project", true)]
        [InlineData("compile with dotnet", true)]
        [InlineData("```bash\nls -la\n```", true)]
        [InlineData("help me understand this", false)]
        [InlineData("refactor this code", false)]
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
        public async Task HandleAsync_WithValidCommand_ReturnsSuccessfulResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "run dotnet build"
            };

            var validationResult = new CommandValidationResult
            {
                IsValid = true,
                SecurityRisk = SecurityRiskLevel.Low
            };

            var sandboxedCommand = new SandboxedCommand
            {
                Executable = "dotnet",
                Arguments = "build",
                WorkingDirectory = Environment.CurrentDirectory
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockSandbox
                .Setup(x => x.PrepareCommandAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sandboxedCommand);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("ShellAgent", result.AgentName);
            Assert.NotNull(result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithInvalidCommand_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "run rm -rf /"
            };

            var validationResult = new CommandValidationResult
            {
                IsValid = false,
                ErrorMessage = "Command is not allowed",
                SecurityRisk = SecurityRiskLevel.Critical
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Some commands failed", result.Message);
            Assert.Contains("Command is not allowed", result.Content);
        }

        [Fact]
        public async Task HandleAsync_WithCodeBlock_ExtractsCommand()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "Please run this:\n```bash\ngit status\n```"
            };

            var validationResult = new CommandValidationResult
            {
                IsValid = true,
                SecurityRisk = SecurityRiskLevel.Low
            };

            var sandboxedCommand = new SandboxedCommand
            {
                Executable = "git",
                Arguments = "status"
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockSandbox
                .Setup(x => x.PrepareCommandAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sandboxedCommand);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            _mockValidator.Verify(
                x => x.ValidateAsync(
                    It.Is<ShellCommand>(c => c.CommandText == "git status"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithMultipleCommands_ExecutesAll()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "```bash\ndotnet build\ndotnet test\n```"
            };

            var validationResult = new CommandValidationResult
            {
                IsValid = true,
                SecurityRisk = SecurityRiskLevel.Low
            };

            var sandboxedCommand = new SandboxedCommand
            {
                Executable = "dotnet",
                Arguments = "build"
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockSandbox
                .Setup(x => x.PrepareCommandAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sandboxedCommand);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            _mockValidator.Verify(
                x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_WithNoCommands_ReturnsFailure()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "This doesn't contain any commands"
            };

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("No valid commands found in the request", result.Message);
        }

        [Fact]
        public async Task HandleAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "run dotnet build"
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to process shell command request", result.Message);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public async Task HandleAsync_WithSecurityRisk_IncludesRiskInformation()
        {
            // Arrange
            var request = new AgentRequest
            {
                Prompt = "run potentially-risky-command"
            };

            var validationResult = new CommandValidationResult
            {
                IsValid = false,
                ErrorMessage = "High security risk",
                SecurityRisk = SecurityRiskLevel.High
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Security Risk: High", result.Content);
        }

        [Theory]
        [InlineData("run dotnet build")]
        [InlineData("execute npm install")]
        [InlineData("command: git status")]
        [InlineData("> ls -la")]
        public async Task HandleAsync_ExtractsCommandsCorrectly(string prompt)
        {
            // Arrange
            var request = new AgentRequest { Prompt = prompt };

            var validationResult = new CommandValidationResult
            {
                IsValid = true,
                SecurityRisk = SecurityRiskLevel.Low
            };

            var sandboxedCommand = new SandboxedCommand
            {
                Executable = "test",
                Arguments = "args"
            };

            _mockValidator
                .Setup(x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            _mockSandbox
                .Setup(x => x.PrepareCommandAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sandboxedCommand);

            // Act
            var result = await _agent.HandleAsync(request);

            // Assert
            Assert.True(result.Success);
            _mockValidator.Verify(
                x => x.ValidateAsync(It.IsAny<ShellCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_LogsInitialization()
        {
            // Act
            await _agent.InitializeAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("ShellAgent initialized")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}