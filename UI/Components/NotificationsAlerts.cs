using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using A3sist.Shared.Messaging;

namespace A3sist.UI.Components
{
    public class MainDashboardPanel
    {
        private readonly INotificationService _notificationService;
        private readonly IAgentStatusService _agentStatusService;
        private readonly ILogger<MainDashboardPanel> _logger;

        public MainDashboardPanel(
            INotificationService notificationService,
            IAgentStatusService agentStatusService,
            ILogger<MainDashboardPanel> logger)
        {
            _notificationService = notificationService;
            _agentStatusService = agentStatusService;
            _logger = logger;
        }

        public async Task<List<AgentStatus>> GetActiveAgentsAsync()
        {
            try
            {
                var agents = await _agentStatusService.GetActiveAgentsAsync();
                _logger.LogInformation($"Retrieved {agents.Count} active agents");
                return agents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active agents");
                return new List<AgentStatus>();
            }
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(int count = 5)
        {
            try
            {
                var notifications = await _notificationService.GetRecentNotificationsAsync(count);
                _logger.LogInformation($"Retrieved {notifications.Count} recent notifications");
                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent notifications");
                return new List<Notification>();
            }
        }

        public async Task<bool> ApplyQuickActionAsync(string actionType, string target)
        {
            try
            {
                var result = await _notificationService.ApplyQuickActionAsync(actionType, target);
                _logger.LogInformation($"Applied quick action {actionType} to {target}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying quick action {actionType}");
                return false;
            }
        }
    }
}