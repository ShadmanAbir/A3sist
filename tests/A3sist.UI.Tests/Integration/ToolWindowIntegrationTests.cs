using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using A3sist.UI.ToolWindows;
using A3sist.UI.Services;
using A3sist.UI.Components;
using A3sist.Shared.Enums;

namespace A3sist.UI.Tests.Integration
{
    [TestClass]
    public class ToolWindowIntegrationTests
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Initialize WPF application context for UI tests
            if (Application.Current == null)
            {
                new Application();
            }
        }

        [TestMethod]
        public async Task ToolWindow_CompleteWorkflow_ProcessesRequestSuccessfully()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            var notificationService = ProgressNotificationService.Instance;
            
            // Clear any existing notifications
            notificationService.ClearNotifications();
            
            viewModel.CurrentRequest = "Analyze this code for potential improvements";
            viewModel.SelectedAgentType = AgentType.Analyzer;

            // Act
            await viewModel.SendRequestCommand.ExecuteAsync(null, null, default);

            // Assert
            Assert.AreEqual(1, viewModel.RequestHistory.Count);
            var historyItem = viewModel.RequestHistory[0];
            Assert.IsNotNull(historyItem.Request);
            Assert.IsNotNull(historyItem.Result);
            Assert.AreEqual("Analyze this code for potential improvements", historyItem.Request.Prompt);
            Assert.AreEqual(AgentType.Analyzer, historyItem.Request.PreferredAgentType);
            Assert.IsTrue(historyItem.Result.Success);
            Assert.AreEqual(string.Empty, viewModel.CurrentRequest); // Should be cleared after processing
        }

        [TestMethod]
        public async Task AgentStatusWindow_RefreshCycle_UpdatesAgentInformation()
        {
            // Arrange
            var statusWindow = new AgentStatusWindowControl();
            
            // Act
            await statusWindow.RefreshAgentStatusesPublicAsync();

            // Assert
            Assert.IsTrue(statusWindow.AgentStatuses.Count > 0);
            Assert.IsNotNull(statusWindow.SystemMetrics);
            Assert.IsTrue(statusWindow.SystemMetrics.TotalAgents > 0);
            Assert.IsNotNull(statusWindow.LastRefreshTime);
            Assert.IsTrue(statusWindow.LastRefreshTime.Contains("Last updated"));
        }

        [TestMethod]
        public void NotificationService_ProgressOperations_IntegrateWithStatusBar()
        {
            // Arrange
            var notificationService = ProgressNotificationService.Instance;
            var initialOperationCount = notificationService.ActiveOperations.Count;

            // Act
            var operation1 = notificationService.StartOperation("Test Operation 1");
            var operation2 = notificationService.StartOperation("Test Operation 2");
            
            operation1.Progress = 50;
            operation2.Progress = 75;

            // Assert
            Assert.AreEqual(initialOperationCount + 2, notificationService.ActiveOperations.Count);
            Assert.IsTrue(notificationService.HasActiveOperations);
            Assert.IsTrue(notificationService.OverallProgress > 0);
            Assert.IsNotNull(notificationService.CurrentOperationText);

            // Complete operations
            operation1.Complete("Operation 1 completed");
            operation2.Complete("Operation 2 completed");

            // Verify completion
            Assert.AreEqual(initialOperationCount, notificationService.ActiveOperations.Count);
        }

        [TestMethod]
        public async Task ToolWindow_ErrorHandling_DisplaysErrorsCorrectly()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            var notificationService = ProgressNotificationService.Instance;
            
            // Simulate an error scenario by setting an empty request
            viewModel.CurrentRequest = "";

            // Act
            await viewModel.SendRequestCommand.ExecuteAsync(null, null, default);

            // Assert - Command should not execute with empty request
            Assert.AreEqual(0, viewModel.RequestHistory.Count);
            Assert.IsFalse(viewModel.IsProcessing);
        }

        [TestMethod]
        public void StatusBarIntegration_LifecycleManagement_WorksCorrectly()
        {
            // Arrange
            StatusBarIntegration statusBar = null;

            // Act & Assert - Constructor should not throw
            Assert.DoesNotThrow(() =>
            {
                statusBar = new StatusBarIntegration();
            });

            // Test message display
            Assert.DoesNotThrow(() =>
            {
                statusBar?.ShowMessage("Test message");
                statusBar?.ShowMessage("Error message", true);
                statusBar?.ClearMessage();
            });

            // Test progress display
            Assert.DoesNotThrow(() =>
            {
                statusBar?.ShowProgress("Test progress", 50, 100);
                statusBar?.HideProgress();
            });

            // Cleanup
            statusBar?.Dispose();
        }

        [TestMethod]
        public async Task ToolWindow_AgentSelection_FiltersCorrectly()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            
            // Populate with test agents
            await viewModel.RefreshAgentsCommand.ExecuteAsync(null, null, default);

            // Act
            viewModel.SelectedAgentType = AgentType.Fixer;
            viewModel.CurrentRequest = "Fix this code issue";
            
            await viewModel.SendRequestCommand.ExecuteAsync(null, null, default);

            // Assert
            var historyItem = viewModel.RequestHistory[0];
            Assert.AreEqual(AgentType.Fixer, historyItem.Request.PreferredAgentType);
        }

        [TestMethod]
        public void AgentStatusDisplayModel_ColorMapping_ReturnsCorrectColors()
        {
            // Arrange
            var model = new AgentStatusDisplayModel();

            // Test all status colors
            var statusTests = new[]
            {
                (WorkStatus.InProgress, System.Windows.Media.Brushes.Blue),
                (WorkStatus.Completed, System.Windows.Media.Brushes.Green),
                (WorkStatus.Failed, System.Windows.Media.Brushes.Red),
                (WorkStatus.Cancelled, System.Windows.Media.Brushes.Orange),
                (WorkStatus.Paused, System.Windows.Media.Brushes.Gray)
            };

            foreach (var (status, expectedColor) in statusTests)
            {
                // Act
                model.Status = status;

                // Assert
                Assert.AreEqual(expectedColor, model.StatusColor, $"Status {status} should have color {expectedColor}");
            }

            // Test all health colors
            var healthTests = new[]
            {
                (HealthStatus.Healthy, System.Windows.Media.Brushes.Green),
                (HealthStatus.Warning, System.Windows.Media.Brushes.Orange),
                (HealthStatus.Critical, System.Windows.Media.Brushes.Red),
                (HealthStatus.Unhealthy, System.Windows.Media.Brushes.DarkRed)
            };

            foreach (var (health, expectedColor) in healthTests)
            {
                // Act
                model.Health = health;

                // Assert
                Assert.AreEqual(expectedColor, model.HealthColor, $"Health {health} should have color {expectedColor}");
            }
        }

        [TestMethod]
        public async Task ToolWindow_ConcurrentOperations_HandledCorrectly()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            var notificationService = ProgressNotificationService.Instance;

            // Act - Start multiple operations concurrently
            var tasks = new[]
            {
                Task.Run(async () =>
                {
                    var op = notificationService.StartOperation("Concurrent Op 1");
                    await Task.Delay(100);
                    op.Complete();
                }),
                Task.Run(async () =>
                {
                    var op = notificationService.StartOperation("Concurrent Op 2");
                    await Task.Delay(150);
                    op.Complete();
                }),
                Task.Run(async () =>
                {
                    var op = notificationService.StartOperation("Concurrent Op 3");
                    await Task.Delay(200);
                    op.Complete();
                })
            };

            // Wait for all operations to complete
            await Task.WhenAll(tasks);

            // Assert - All operations should complete without issues
            Assert.IsFalse(notificationService.HasActiveOperations);
        }

        [TestMethod]
        public void NotificationPanel_UIElements_CreatedCorrectly()
        {
            // Arrange & Act
            var panel = new A3sist.UI.Controls.NotificationPanel();

            // Assert
            Assert.IsNotNull(panel.Content);
            Assert.IsInstanceOfType(panel.Content, typeof(System.Windows.Controls.DockPanel));
            
            // Verify the panel can be added to a parent container
            var parentPanel = new System.Windows.Controls.StackPanel();
            Assert.DoesNotThrow(() => parentPanel.Children.Add(panel));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up any test data
            ProgressNotificationService.Instance.ClearNotifications();
            
            // Clear any active operations
            var activeOps = ProgressNotificationService.Instance.ActiveOperations.ToArray();
            foreach (var op in activeOps)
            {
                if (op.IsActive)
                {
                    op.Complete("Test cleanup");
                }
            }
        }
    }

    /// <summary>
    /// Helper class for testing assertions that don't throw exceptions
    /// </summary>
    public static class AssertExtensions
    {
        public static void DoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex}");
            }
        }
    }
}