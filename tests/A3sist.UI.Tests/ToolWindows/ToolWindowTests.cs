using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using A3sist.UI.ToolWindows;
using A3sist.UI.Services;
using A3sist.UI.Controls;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Messaging;
using System.ComponentModel;

namespace A3sist.UI.Tests.ToolWindows
{
    [TestClass]
    public class ToolWindowTests
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

        #region A3ToolWindowData Tests

        [TestMethod]
        public void A3ToolWindowData_Constructor_InitializesPropertiesCorrectly()
        {
            // Arrange & Act
            var viewModel = new A3ToolWindowData();

            // Assert
            Assert.IsNotNull(viewModel.SendRequestCommand);
            Assert.IsNotNull(viewModel.ClearHistoryCommand);
            Assert.IsNotNull(viewModel.RefreshAgentsCommand);
            Assert.IsNotNull(viewModel.RequestHistory);
            Assert.IsNotNull(viewModel.AvailableAgents);
            Assert.AreEqual(string.Empty, viewModel.CurrentRequest);
            Assert.AreEqual("Ready", viewModel.StatusMessage);
            Assert.IsFalse(viewModel.IsProcessing);
            Assert.AreEqual(AgentType.Unknown, viewModel.SelectedAgentType);
        }

        [TestMethod]
        public void A3ToolWindowData_PropertyChanges_RaisePropertyChangedEvents()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            var propertyChangedEvents = new List<string>();
            viewModel.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName);

            // Act
            viewModel.CurrentRequest = "Test request";
            viewModel.StatusMessage = "Processing";
            viewModel.IsProcessing = true;
            viewModel.ProgressValue = 50;

            // Assert
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(viewModel.CurrentRequest)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(viewModel.StatusMessage)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(viewModel.IsProcessing)));
            Assert.IsTrue(propertyChangedEvents.Contains(nameof(viewModel.ProgressValue)));
        }

        [TestMethod]
        public async Task A3ToolWindowData_ClearHistoryCommand_ClearsRequestHistory()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            viewModel.RequestHistory.Add(new AgentRequestHistoryItem
            {
                Request = new AgentRequest("Test"),
                Timestamp = DateTime.Now,
                Status = "Completed"
            });

            // Act
            await viewModel.ClearHistoryCommand.ExecuteAsync(null, null, default);

            // Assert
            Assert.AreEqual(0, viewModel.RequestHistory.Count);
            Assert.AreEqual("History cleared", viewModel.StatusMessage);
        }

        [TestMethod]
        public async Task A3ToolWindowData_RefreshAgentsCommand_PopulatesAvailableAgents()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();

            // Act
            await viewModel.RefreshAgentsCommand.ExecuteAsync(null, null, default);

            // Assert
            Assert.IsTrue(viewModel.AvailableAgents.Count > 0);
            Assert.IsTrue(viewModel.StatusMessage.Contains("agents"));
        }

        #endregion

        #region AgentStatusWindowControl Tests

        [TestMethod]
        public void AgentStatusWindowControl_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var control = new AgentStatusWindowControl();

            // Assert
            Assert.IsNotNull(control.AgentStatuses);
            Assert.IsNotNull(control.SystemMetrics);
            Assert.IsFalse(control.IsRefreshing);
        }

        [TestMethod]
        public void AgentStatusWindowControl_SystemMetrics_UpdatesCorrectly()
        {
            // Arrange
            var control = new AgentStatusWindowControl();
            var propertyChangedEvents = new List<string>();
            control.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName);

            // Act
            control.SystemMetrics.TotalAgents = 5;
            control.SystemMetrics.ActiveAgents = 3;
            control.SystemMetrics.OverallHealth = "Healthy";

            // Assert
            Assert.AreEqual(5, control.SystemMetrics.TotalAgents);
            Assert.AreEqual(3, control.SystemMetrics.ActiveAgents);
            Assert.AreEqual("Healthy", control.SystemMetrics.OverallHealth);
        }

        #endregion

        #region AgentStatusDisplayModel Tests

        [TestMethod]
        public void AgentStatusDisplayModel_StatusColor_ReturnsCorrectBrush()
        {
            // Arrange
            var model = new AgentStatusDisplayModel();

            // Act & Assert
            model.Status = WorkStatus.InProgress;
            Assert.AreEqual(System.Windows.Media.Brushes.Blue, model.StatusColor);

            model.Status = WorkStatus.Completed;
            Assert.AreEqual(System.Windows.Media.Brushes.Green, model.StatusColor);

            model.Status = WorkStatus.Failed;
            Assert.AreEqual(System.Windows.Media.Brushes.Red, model.StatusColor);
        }

        [TestMethod]
        public void AgentStatusDisplayModel_HealthColor_ReturnsCorrectBrush()
        {
            // Arrange
            var model = new AgentStatusDisplayModel();

            // Act & Assert
            model.Health = HealthStatus.Healthy;
            Assert.AreEqual(System.Windows.Media.Brushes.Green, model.HealthColor);

            model.Health = HealthStatus.Warning;
            Assert.AreEqual(System.Windows.Media.Brushes.Orange, model.HealthColor);

            model.Health = HealthStatus.Critical;
            Assert.AreEqual(System.Windows.Media.Brushes.Red, model.HealthColor);
        }

        [TestMethod]
        public void AgentStatusDisplayModel_MetricsDisplay_FormatsCorrectly()
        {
            // Arrange
            var model = new AgentStatusDisplayModel
            {
                TasksProcessed = 100,
                TasksSucceeded = 95,
                TasksFailed = 5,
                AverageProcessingTime = TimeSpan.FromSeconds(2.5)
            };

            // Act
            var display = model.MetricsDisplay;

            // Assert
            Assert.IsTrue(display.Contains("Tasks: 100"));
            Assert.IsTrue(display.Contains("Success: 95.0%"));
            Assert.IsTrue(display.Contains("Avg: 2.5s"));
        }

        #endregion

        #region ProgressNotificationService Tests

        [TestMethod]
        public void ProgressNotificationService_Instance_ReturnsSameInstance()
        {
            // Arrange & Act
            var instance1 = ProgressNotificationService.Instance;
            var instance2 = ProgressNotificationService.Instance;

            // Assert
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void ProgressNotificationService_StartOperation_AddsToActiveOperations()
        {
            // Arrange
            var service = ProgressNotificationService.Instance;
            var initialCount = service.ActiveOperations.Count;

            // Act
            var operation = service.StartOperation("Test operation");

            // Assert
            Assert.AreEqual(initialCount + 1, service.ActiveOperations.Count);
            Assert.IsTrue(service.HasActiveOperations);
            Assert.AreEqual("Test operation", service.CurrentOperationText);
        }

        [TestMethod]
        public void ProgressNotificationService_CompleteOperation_RemovesFromActiveOperations()
        {
            // Arrange
            var service = ProgressNotificationService.Instance;
            var operation = service.StartOperation("Test operation");
            var initialCount = service.ActiveOperations.Count;

            // Act
            operation.Complete("Operation completed");

            // Assert
            Assert.AreEqual(initialCount - 1, service.ActiveOperations.Count);
        }

        [TestMethod]
        public void ProgressNotificationService_ShowSuccess_AddsNotification()
        {
            // Arrange
            var service = ProgressNotificationService.Instance;
            var initialCount = service.Notifications.Count;

            // Act
            service.ShowSuccess("Test Success", "Success message");

            // Assert
            Assert.AreEqual(initialCount + 1, service.Notifications.Count);
            var notification = service.Notifications.First();
            Assert.AreEqual("Test Success", notification.Title);
            Assert.AreEqual("Success message", notification.Message);
            Assert.AreEqual(NotificationType.Success, notification.Type);
        }

        [TestMethod]
        public void ProgressNotificationService_ShowError_AddsErrorNotification()
        {
            // Arrange
            var service = ProgressNotificationService.Instance;
            var initialCount = service.Notifications.Count;

            // Act
            service.ShowError("Test Error", "Error message");

            // Assert
            Assert.AreEqual(initialCount + 1, service.Notifications.Count);
            var notification = service.Notifications.First();
            Assert.AreEqual("Test Error", notification.Title);
            Assert.AreEqual("Error message", notification.Message);
            Assert.AreEqual(NotificationType.Error, notification.Type);
        }

        #endregion

        #region ProgressOperation Tests

        [TestMethod]
        public void ProgressOperation_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var operation = new ProgressOperation("Test operation", false);

            // Assert
            Assert.AreEqual("Test operation", operation.Description);
            Assert.IsFalse(operation.IsIndeterminate);
            Assert.IsTrue(operation.IsActive);
            Assert.IsTrue(operation.IsSuccess);
            Assert.AreEqual(0, operation.Progress);
        }

        [TestMethod]
        public void ProgressOperation_SetProgress_ClampsToValidRange()
        {
            // Arrange
            var operation = new ProgressOperation("Test operation");

            // Act & Assert
            operation.Progress = -10;
            Assert.AreEqual(0, operation.Progress);

            operation.Progress = 150;
            Assert.AreEqual(100, operation.Progress);

            operation.Progress = 50;
            Assert.AreEqual(50, operation.Progress);
        }

        [TestMethod]
        public void ProgressOperation_Complete_SetsCorrectState()
        {
            // Arrange
            var operation = new ProgressOperation("Test operation");
            var completedEventFired = false;
            operation.Completed += (s, e) => completedEventFired = true;

            // Act
            operation.Complete("Test completed");

            // Assert
            Assert.IsFalse(operation.IsActive);
            Assert.IsTrue(operation.IsSuccess);
            Assert.AreEqual("Test completed", operation.StatusMessage);
            Assert.AreEqual(100, operation.Progress);
            Assert.IsTrue(completedEventFired);
        }

        [TestMethod]
        public void ProgressOperation_Fail_SetsCorrectState()
        {
            // Arrange
            var operation = new ProgressOperation("Test operation");
            var completedEventFired = false;
            operation.Completed += (s, e) => completedEventFired = true;

            // Act
            operation.Fail("Test failed");

            // Assert
            Assert.IsFalse(operation.IsActive);
            Assert.IsFalse(operation.IsSuccess);
            Assert.AreEqual("Test failed", operation.StatusMessage);
            Assert.IsTrue(completedEventFired);
        }

        #endregion

        #region NotificationItem Tests

        [TestMethod]
        public void NotificationItem_TimeAgo_FormatsCorrectly()
        {
            // Arrange
            var notification = new NotificationItem
            {
                Timestamp = DateTime.Now.AddMinutes(-30)
            };

            // Act
            var timeAgo = notification.TimeAgo;

            // Assert
            Assert.IsTrue(timeAgo.Contains("30m ago") || timeAgo.Contains("m ago"));
        }

        [TestMethod]
        public void NotificationItem_IsRead_RaisesPropertyChanged()
        {
            // Arrange
            var notification = new NotificationItem();
            var propertyChangedFired = false;
            notification.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(notification.IsRead))
                    propertyChangedFired = true;
            };

            // Act
            notification.IsRead = true;

            // Assert
            Assert.IsTrue(propertyChangedFired);
            Assert.IsTrue(notification.IsRead);
        }

        #endregion

        #region NotificationPanel Tests

        [TestMethod]
        public void NotificationPanel_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var panel = new NotificationPanel();

            // Assert
            Assert.IsNotNull(panel.Content);
            Assert.IsInstanceOfType(panel.Content, typeof(DockPanel));
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public async Task ToolWindow_Integration_SendRequestUpdatesHistory()
        {
            // Arrange
            var viewModel = new A3ToolWindowData();
            viewModel.CurrentRequest = "Test integration request";

            // Act
            await viewModel.SendRequestCommand.ExecuteAsync(null, null, default);

            // Assert
            Assert.AreEqual(1, viewModel.RequestHistory.Count);
            var historyItem = viewModel.RequestHistory.First();
            Assert.AreEqual("Test integration request", historyItem.Request.Prompt);
            Assert.IsNotNull(historyItem.Result);
        }

        [TestMethod]
        public void AgentStatusWindow_Integration_DisplaysAgentData()
        {
            // Arrange
            var control = new AgentStatusWindowControl();

            // Act
            control.AgentStatuses.Add(new AgentStatusDisplayModel
            {
                Name = "TestAgent",
                Type = AgentType.Fixer,
                Status = WorkStatus.InProgress,
                Health = HealthStatus.Healthy,
                TasksProcessed = 10,
                TasksSucceeded = 9,
                TasksFailed = 1
            });

            // Assert
            Assert.AreEqual(1, control.AgentStatuses.Count);
            var agent = control.AgentStatuses.First();
            Assert.AreEqual("TestAgent", agent.Name);
            Assert.AreEqual(AgentType.Fixer, agent.Type);
            Assert.AreEqual(WorkStatus.InProgress, agent.Status);
            Assert.AreEqual(HealthStatus.Healthy, agent.Health);
        }

        #endregion

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up any test data
            ProgressNotificationService.Instance.ClearNotifications();
        }
    }
}