using A3sist.Core.Agents.Base;
using System;
using System.Threading.Tasks;
using Xunit;

namespace A3sist.Core.Tests.Agents.Base
{
    public class AgentMetricsTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            // Act
            var metrics = new AgentMetrics();

            // Assert
            Assert.Equal(0, metrics.TasksProcessed);
            Assert.Equal(0, metrics.TasksSucceeded);
            Assert.Equal(0, metrics.TasksFailed);
            Assert.Equal(TimeSpan.Zero, metrics.AverageProcessingTime);
            Assert.Equal(0.0, metrics.SuccessRate);
            Assert.Equal(0.0, metrics.FailureRate);
            Assert.True(metrics.LastActivity <= DateTime.UtcNow);
        }

        [Fact]
        public void IncrementTasksProcessed_IncrementsCounter()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Act
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksProcessed();

            // Assert
            Assert.Equal(2, metrics.TasksProcessed);
        }

        [Fact]
        public void IncrementTasksSucceeded_IncrementsCounter()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Act
            metrics.IncrementTasksSucceeded();
            metrics.IncrementTasksSucceeded();
            metrics.IncrementTasksSucceeded();

            // Assert
            Assert.Equal(3, metrics.TasksSucceeded);
        }

        [Fact]
        public void IncrementTasksFailed_IncrementsCounter()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Act
            metrics.IncrementTasksFailed();

            // Assert
            Assert.Equal(1, metrics.TasksFailed);
        }

        [Fact]
        public void UpdateAverageProcessingTime_CalculatesCorrectAverage()
        {
            // Arrange
            var metrics = new AgentMetrics();
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksProcessed();

            // Act
            metrics.UpdateAverageProcessingTime(TimeSpan.FromMilliseconds(100));
            metrics.UpdateAverageProcessingTime(TimeSpan.FromMilliseconds(200));

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(150), metrics.AverageProcessingTime);
        }

        [Fact]
        public void SuccessRate_CalculatesCorrectRate()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Act
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksSucceeded();
            metrics.IncrementTasksSucceeded();

            // Assert
            Assert.Equal(2.0 / 3.0, metrics.SuccessRate, 2);
        }

        [Fact]
        public void FailureRate_CalculatesCorrectRate()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Act
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksFailed();

            // Assert
            Assert.Equal(1.0 / 3.0, metrics.FailureRate, 2);
        }

        [Fact]
        public void SuccessRate_WithNoTasks_ReturnsZero()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Assert
            Assert.Equal(0.0, metrics.SuccessRate);
        }

        [Fact]
        public void FailureRate_WithNoTasks_ReturnsZero()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Assert
            Assert.Equal(0.0, metrics.FailureRate);
        }

        [Fact]
        public void AverageProcessingTime_WithNoTasks_ReturnsZero()
        {
            // Arrange
            var metrics = new AgentMetrics();

            // Assert
            Assert.Equal(TimeSpan.Zero, metrics.AverageProcessingTime);
        }

        [Fact]
        public void Reset_ResetsAllMetrics()
        {
            // Arrange
            var metrics = new AgentMetrics();
            metrics.IncrementTasksProcessed();
            metrics.IncrementTasksSucceeded();
            metrics.IncrementTasksFailed();
            metrics.UpdateAverageProcessingTime(TimeSpan.FromMilliseconds(100));

            // Act
            metrics.Reset();

            // Assert
            Assert.Equal(0, metrics.TasksProcessed);
            Assert.Equal(0, metrics.TasksSucceeded);
            Assert.Equal(0, metrics.TasksFailed);
            Assert.Equal(TimeSpan.Zero, metrics.AverageProcessingTime);
        }

        [Fact]
        public void LastActivity_UpdatesOnMetricChanges()
        {
            // Arrange
            var metrics = new AgentMetrics();
            var initialActivity = metrics.LastActivity;

            // Act
            Task.Delay(10).Wait(); // Small delay to ensure time difference
            metrics.IncrementTasksProcessed();

            // Assert
            Assert.True(metrics.LastActivity > initialActivity);
        }

        [Fact]
        public void ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var metrics = new AgentMetrics();
            const int taskCount = 1000;

            // Act
            Parallel.For(0, taskCount, i =>
            {
                metrics.IncrementTasksProcessed();
                if (i % 2 == 0)
                    metrics.IncrementTasksSucceeded();
                else
                    metrics.IncrementTasksFailed();
                
                metrics.UpdateAverageProcessingTime(TimeSpan.FromMilliseconds(i));
            });

            // Assert
            Assert.Equal(taskCount, metrics.TasksProcessed);
            Assert.Equal(taskCount / 2, metrics.TasksSucceeded);
            Assert.Equal(taskCount / 2, metrics.TasksFailed);
            Assert.True(metrics.AverageProcessingTime > TimeSpan.Zero);
        }
    }
}