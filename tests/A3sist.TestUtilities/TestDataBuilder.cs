using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;

namespace A3sist.TestUtilities;

/// <summary>
/// Builder pattern for creating test data objects
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Builder for AgentRequest objects
    /// </summary>
    public class AgentRequestBuilder
    {
        private readonly AgentRequest _request;

        public AgentRequestBuilder()
        {
            _request = new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Test prompt",
                FilePath = "test.cs",
                Content = "// Test content",
                Context = new Dictionary<string, object>(),
                PreferredAgentType = AgentType.Analyzer,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };
        }

        public AgentRequestBuilder WithId(Guid id)
        {
            _request.Id = id;
            return this;
        }

        public AgentRequestBuilder WithPrompt(string prompt)
        {
            _request.Prompt = prompt;
            return this;
        }

        public AgentRequestBuilder WithFilePath(string filePath)
        {
            _request.FilePath = filePath;
            return this;
        }

        public AgentRequestBuilder WithContent(string content)
        {
            _request.Content = content;
            return this;
        }

        public AgentRequestBuilder WithContext(string key, object value)
        {
            _request.Context[key] = value;
            return this;
        }

        public AgentRequestBuilder WithAgentType(AgentType agentType)
        {
            _request.PreferredAgentType = agentType;
            return this;
        }

        public AgentRequestBuilder WithUserId(string userId)
        {
            _request.UserId = userId;
            return this;
        }

        public AgentRequest Build() => _request;
    }

    /// <summary>
    /// Builder for AgentResult objects
    /// </summary>
    public class AgentResultBuilder
    {
        private readonly AgentResult _result;

        public AgentResultBuilder()
        {
            _result = new AgentResult
            {
                Success = true,
                Message = "Test result",
                Content = "Test content",
                Metadata = new Dictionary<string, object>(),
                ProcessingTime = TimeSpan.FromMilliseconds(100),
                AgentName = "TestAgent"
            };
        }

        public AgentResultBuilder WithSuccess(bool success)
        {
            _result.Success = success;
            return this;
        }

        public AgentResultBuilder WithMessage(string message)
        {
            _result.Message = message;
            return this;
        }

        public AgentResultBuilder WithContent(string content)
        {
            _result.Content = content;
            return this;
        }

        public AgentResultBuilder WithException(Exception exception)
        {
            _result.Exception = exception;
            _result.Success = false;
            return this;
        }

        public AgentResultBuilder WithProcessingTime(TimeSpan processingTime)
        {
            _result.ProcessingTime = processingTime;
            return this;
        }

        public AgentResultBuilder WithAgentName(string agentName)
        {
            _result.AgentName = agentName;
            return this;
        }

        public AgentResultBuilder WithMetadata(string key, object value)
        {
            _result.Metadata[key] = value;
            return this;
        }

        public AgentResult Build() => _result;
    }

    /// <summary>
    /// Builder for AgentStatus objects
    /// </summary>
    public class AgentStatusBuilder
    {
        private readonly AgentStatus _status;

        public AgentStatusBuilder()
        {
            _status = new AgentStatus
            {
                Name = "TestAgent",
                Type = AgentType.Analyzer,
                Status = WorkStatus.Pending,
                LastActivity = DateTime.UtcNow,
                TasksProcessed = 0,
                TasksSucceeded = 0,
                TasksFailed = 0,
                AverageProcessingTime = TimeSpan.FromMilliseconds(100)
            };
        }

        public AgentStatusBuilder WithName(string name)
        {
            _status.Name = name;
            return this;
        }

        public AgentStatusBuilder WithType(AgentType type)
        {
            _status.Type = type;
            return this;
        }

        public AgentStatusBuilder WithStatus(WorkStatus status)
        {
            _status.Status = status;
            return this;
        }

        public AgentStatusBuilder WithTaskCounts(int processed, int succeeded, int failed)
        {
            _status.TasksProcessed = processed;
            _status.TasksSucceeded = succeeded;
            _status.TasksFailed = failed;
            return this;
        }

        public AgentStatusBuilder WithAverageProcessingTime(TimeSpan averageTime)
        {
            _status.AverageProcessingTime = averageTime;
            return this;
        }

        public AgentStatus Build() => _status;
    }

    /// <summary>
    /// Creates a new AgentRequest builder
    /// </summary>
    public static AgentRequestBuilder AgentRequest() => new();

    /// <summary>
    /// Creates a new AgentResult builder
    /// </summary>
    public static AgentResultBuilder AgentResult() => new();

    /// <summary>
    /// Creates a new AgentStatus builder
    /// </summary>
    public static AgentStatusBuilder AgentStatus() => new();
}