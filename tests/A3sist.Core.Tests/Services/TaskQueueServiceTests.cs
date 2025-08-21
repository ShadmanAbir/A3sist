using A3sist.Core.Services;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Services
{
    public class TaskQueueServiceTests : IDisposable
    {
        private readonly Mock<ILogger<TaskQueueService>> _mockLogger;
        private readonly TaskQueueService _taskQueueService;

        public TaskQueueServiceTests()
        {
            _mockLogger = new Mock<ILogger<TaskQueueService>>();
            _taskQueueService = new TaskQueueService(_mockLogger.Object);
        }

        [Fact]
        public async Task EnqueueAsync_WithValidRequest_ShouldEnqueueSuccessfully()
        {
            // Arrange
            var request = CreateValidRequest();
            var eventRaised = false;
            
            _taskQueueService.TaskEnqueued += (sender, args) =>
            {
                eventRaised = true;
                Assert.Equal(request.Id, args.Request.Id);
                Assert.Equal(TaskPriority.Normal, args.Priority);
            };

            // Act
            await _taskQueueService.EnqueueAsync(request);

            // Assert
            var queueSize = await _taskQueueService.GetQueueSizeAsync();
            Assert.Equal(1, queueSize);
            Assert.True(eventRaised);
        }

        [Fact]
        public async Task EnqueueAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _taskQueueService.EnqueueAsync(null!));
        }

        [Fact]
        public async Task DequeueAsync_WithItemInQueue_ShouldReturnItem()
        {
            // Arrange
            var request = CreateValidRequest();
            var eventRaised = false;
            
            _taskQueueService.TaskDequeued += (sender, args) =>
            {
                eventRaised = true;
                Assert.Equal(request.Id, args.Request.Id);
                Assert.Equal(TaskPriority.Normal, args.Priority);
            };

            await _taskQueueService.EnqueueAsync(request);

            // Act
            var dequeuedRequest = await _taskQueueService.DequeueAsync();

            // Assert
            Assert.NotNull(dequeuedRequest);
            Assert.Equal(request.Id, dequeuedRequest.Id);
            Assert.True(eventRaised);
            
            var queueSize = await _taskQueueService.GetQueueSizeAsync();
            Assert.Equal(0, queueSize);
        }

        [Fact]
        public async Task DequeueAsync_WithEmptyQueue_ShouldWaitForItem()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act
            var result = await _taskQueueService.DequeueAsync(cts.Token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task PriorityOrdering_ShouldDequeueHighestPriorityFirst()
        {
            // Arrange
            var lowPriorityRequest = CreateValidRequest();
            var highPriorityRequest = CreateValidRequest();
            var criticalPriorityRequest = CreateValidRequest();

            // Enqueue in mixed order
            await _taskQueueService.EnqueueAsync(lowPriorityRequest, TaskPriority.Low);
            await _taskQueueService.EnqueueAsync(criticalPriorityRequest, TaskPriority.Critical);
            await _taskQueueService.EnqueueAsync(highPriorityRequest, TaskPriority.High);

            // Act & Assert
            var first = await _taskQueueService.DequeueAsync();
            var second = await _taskQueueService.DequeueAsync();
            var third = await _taskQueueService.DequeueAsync();

            Assert.Equal(criticalPriorityRequest.Id, first!.Id);
            Assert.Equal(highPriorityRequest.Id, second!.Id);
            Assert.Equal(lowPriorityRequest.Id, third!.Id);
        }

        [Fact]
        public async Task GetQueueSizeAsync_ShouldReturnCorrectSize()
        {
            // Arrange
            Assert.Equal(0, await _taskQueueService.GetQueueSizeAsync());

            // Act
            await _taskQueueService.EnqueueAsync(CreateValidRequest());
            await _taskQueueService.EnqueueAsync(CreateValidRequest());
            await _taskQueueService.EnqueueAsync(CreateValidRequest());

            // Assert
            Assert.Equal(3, await _taskQueueService.GetQueueSizeAsync());
        }

        [Fact]
        public async Task GetStatisticsAsync_ShouldReturnValidStatistics()
        {
            // Arrange
            await _taskQueueService.EnqueueAsync(CreateValidRequest(), TaskPriority.High);
            await _taskQueueService.EnqueueAsync(CreateValidRequest(), TaskPriority.Normal);
            await _taskQueueService.EnqueueAsync(CreateValidRequest(), TaskPriority.Low);

            // Act
            var statistics = await _taskQueueService.GetStatisticsAsync();

            // Assert
            Assert.NotNull(statistics);
            Assert.Equal(3, statistics.TotalItems);
            Assert.True(statistics.ItemsByPriority.ContainsKey(TaskPriority.High));
            Assert.True(statistics.ItemsByPriority.ContainsKey(TaskPriority.Normal));
            Assert.True(statistics.ItemsByPriority.ContainsKey(TaskPriority.Low));
            Assert.Equal(1, statistics.ItemsByPriority[TaskPriority.High]);
            Assert.Equal(1, statistics.ItemsByPriority[TaskPriority.Normal]);
            Assert.Equal(1, statistics.ItemsByPriority[TaskPriority.Low]);
        }

        [Fact]
        public async Task ClearAsync_ShouldRemoveAllItems()
        {
            // Arrange
            await _taskQueueService.EnqueueAsync(CreateValidRequest());
            await _taskQueueService.EnqueueAsync(CreateValidRequest());
            await _taskQueueService.EnqueueAsync(CreateValidRequest());
            
            Assert.Equal(3, await _taskQueueService.GetQueueSizeAsync());

            // Act
            await _taskQueueService.ClearAsync();

            // Assert
            Assert.Equal(0, await _taskQueueService.GetQueueSizeAsync());
        }

        [Fact]
        public async Task ConcurrentOperations_ShouldHandleCorrectly()
        {
            // Arrange
            const int itemCount = 100;
            var enqueueTasks = new Task[itemCount];
            var dequeueTasks = new Task<AgentRequest?>[itemCount];

            // Act - Enqueue items concurrently
            for (int i = 0; i < itemCount; i++)
            {
                enqueueTasks[i] = _taskQueueService.EnqueueAsync(CreateValidRequest());
            }
            await Task.WhenAll(enqueueTasks);

            // Dequeue items concurrently
            for (int i = 0; i < itemCount; i++)
            {
                dequeueTasks[i] = _taskQueueService.DequeueAsync();
            }
            var results = await Task.WhenAll(dequeueTasks);

            // Assert
            Assert.Equal(itemCount, results.Length);
            Assert.All(results, result => Assert.NotNull(result));
            Assert.Equal(0, await _taskQueueService.GetQueueSizeAsync());
        }

        private static AgentRequest CreateValidRequest()
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid(),
                Prompt = "Test prompt",
                FilePath = "test.cs",
                Content = "test content",
                PreferredAgentType = AgentType.Unknown,
                CreatedAt = DateTime.UtcNow,
                UserId = "test-user"
            };
        }

        public void Dispose()
        {
            _taskQueueService?.Dispose();
        }
    }
}