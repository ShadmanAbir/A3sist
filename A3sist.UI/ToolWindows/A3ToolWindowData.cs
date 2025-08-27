﻿using Microsoft.VisualStudio.Extensibility.UI;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using A3sist.Shared.Models;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;

namespace A3sist.UI
{
    /// <summary>
    /// ViewModel for the main A3sist agent interaction tool window
    /// </summary>
    [DataContract]
    internal class A3ToolWindowData : NotifyPropertyChangedObject
    {
        public A3ToolWindowData()
        {
            // Initialize commands
            SendRequestCommand = new AsyncCommand(ExecuteSendRequestAsync);
            ClearHistoryCommand = new AsyncCommand(ExecuteClearHistoryAsync);
            RefreshAgentsCommand = new AsyncCommand(ExecuteRefreshAgentsAsync);
            
            // Initialize collections
            RequestHistory = new ObservableCollection<AgentRequestHistoryItem>();
            AvailableAgents = new ObservableCollection<AgentStatusViewModel>();
            
            // Initialize properties
            CurrentRequest = string.Empty;
            StatusMessage = "Ready";
            IsProcessing = false;
            SelectedAgentType = AgentType.Unknown;
        }

        #region Properties

        private string _currentRequest = string.Empty;
        [DataMember]
        public string CurrentRequest
        {
            get => _currentRequest;
            set => SetProperty(ref _currentRequest, value);
        }

        private string _statusMessage = string.Empty;
        [DataMember]
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isProcessing;
        [DataMember]
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        private double _progressValue;
        [DataMember]
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private bool _isProgressVisible;
        [DataMember]
        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set => SetProperty(ref _isProgressVisible, value);
        }

        private AgentType _selectedAgentType;
        [DataMember]
        public AgentType SelectedAgentType
        {
            get => _selectedAgentType;
            set => SetProperty(ref _selectedAgentType, value);
        }

        private string _currentFilePath = string.Empty;
        [DataMember]
        public string CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        [DataMember]
        public ObservableCollection<AgentRequestHistoryItem> RequestHistory { get; }

        [DataMember]
        public ObservableCollection<AgentStatusViewModel> AvailableAgents { get; }

        #endregion

        #region Commands

        [DataMember]
        public AsyncCommand SendRequestCommand { get; }

        [DataMember]
        public AsyncCommand ClearHistoryCommand { get; }

        [DataMember]
        public AsyncCommand RefreshAgentsCommand { get; }

        #endregion

        #region Command Implementations

        private async Task ExecuteSendRequestAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(CurrentRequest) || IsProcessing)
                return;

            try
            {
                IsProcessing = true;
                IsProgressVisible = true;
                ProgressValue = 0;
                StatusMessage = "Processing request...";

                // Create the request
                var request = new AgentRequest(CurrentRequest)
                {
                    FilePath = CurrentFilePath,
                    PreferredAgentType = SelectedAgentType != AgentType.Unknown ? SelectedAgentType : null
                };

                // Add to history
                var historyItem = new AgentRequestHistoryItem
                {
                    Request = request,
                    Timestamp = DateTime.Now,
                    Status = "Processing..."
                };
                RequestHistory.Insert(0, historyItem);

                // Simulate progress updates
                for (int i = 0; i <= 100; i += 10)
                {
                    ProgressValue = i;
                    await Task.Delay(100, cancellationToken);
                }

                // TODO: Integrate with actual orchestrator service
                // For now, simulate a response
                var result = new AgentResult
                {
                    Success = true,
                    Message = "Request processed successfully",
                    Content = $"Processed: {CurrentRequest}",
                    AgentName = "SimulatedAgent",
                    ProcessingTime = TimeSpan.FromSeconds(1)
                };

                historyItem.Result = result;
                historyItem.Status = result.Success ? "Completed" : "Failed";

                StatusMessage = result.Success ? "Request completed successfully" : "Request failed";
                CurrentRequest = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                IsProgressVisible = false;
                ProgressValue = 0;
            }
        }

        private async Task ExecuteClearHistoryAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken)
        {
            RequestHistory.Clear();
            StatusMessage = "History cleared";
            await Task.CompletedTask;
        }

        private async Task ExecuteRefreshAgentsAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken)
        {
            try
            {
                StatusMessage = "Refreshing agents...";
                
                // TODO: Integrate with actual agent manager service
                // For now, simulate some agents
                AvailableAgents.Clear();
                
                var simulatedAgents = new[]
                {
                    new AgentStatusViewModel { Name = "CSharpAgent", Type = AgentType.Language, Status = WorkStatus.Completed, Health = HealthStatus.Healthy },
                    new AgentStatusViewModel { Name = "AnalyzerAgent", Type = AgentType.Analyzer, Status = WorkStatus.Pending, Health = HealthStatus.Healthy },
                    new AgentStatusViewModel { Name = "RefactorAgent", Type = AgentType.Refactor, Status = WorkStatus.InProgress, Health = HealthStatus.Warning },
                    new AgentStatusViewModel { Name = "DesignerAgent", Type = AgentType.Designer, Status = WorkStatus.Completed, Health = HealthStatus.Healthy }
                };

                foreach (var agent in simulatedAgents)
                {
                    AvailableAgents.Add(agent);
                }

                StatusMessage = $"Found {AvailableAgents.Count} agents";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing agents: {ex.Message}";
            }
        }

        #endregion
    }

    /// <summary>
    /// View model for agent request history items
    /// </summary>
    [DataContract]
    public class AgentRequestHistoryItem : NotifyPropertyChangedObject
    {
        private AgentRequest _request;
        [DataMember]
        public AgentRequest Request
        {
            get => _request;
            set => SetProperty(ref _request, value);
        }

        private AgentResult _result;
        [DataMember]
        public AgentResult Result
        {
            get => _result;
            set => SetProperty(ref _result, value);
        }

        private DateTime _timestamp;
        [DataMember]
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        private string _status;
        [DataMember]
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }

    /// <summary>
    /// View model for agent status display
    /// </summary>
    [DataContract]
    public class AgentStatusViewModel : NotifyPropertyChangedObject
    {
        private string _name;
        [DataMember]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private AgentType _type;
        [DataMember]
        public AgentType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private WorkStatus _status;
        [DataMember]
        public WorkStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private HealthStatus _health;
        [DataMember]
        public HealthStatus Health
        {
            get => _health;
            set => SetProperty(ref _health, value);
        }

        private int _tasksProcessed;
        [DataMember]
        public int TasksProcessed
        {
            get => _tasksProcessed;
            set => SetProperty(ref _tasksProcessed, value);
        }

        private double _successRate;
        [DataMember]
        public double SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }
    }
}
