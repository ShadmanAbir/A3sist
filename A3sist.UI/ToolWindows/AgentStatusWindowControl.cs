using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using A3sist.Shared.Models;
using A3sist.Shared.Enums;
using A3sist.Shared.Interfaces;

namespace A3sist.UI.ToolWindows
{
    /// <summary>
    /// Comprehensive agent status monitoring control
    /// </summary>
    public partial class AgentStatusWindowControl : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _refreshTimer;
        private IAgentManager _agentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentStatusWindowControl"/> class.
        /// </summary>
        public AgentStatusWindowControl()
        {
            AgentStatuses = new ObservableCollection<AgentStatusDisplayModel>();
            SystemMetrics = new SystemMetricsModel();
            
            InitializeComponent();
            
            // Set up auto-refresh timer
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshAgentStatusesAsync();
            
            // Start monitoring
            _refreshTimer.Start();
            
            // Initial load
            Loaded += async (s, e) => await RefreshAgentStatusesAsync();
        }

        #region Properties

        public ObservableCollection<AgentStatusDisplayModel> AgentStatuses { get; }

        private SystemMetricsModel _systemMetrics;
        public SystemMetricsModel SystemMetrics
        {
            get => _systemMetrics;
            set
            {
                _systemMetrics = value;
                OnPropertyChanged(nameof(SystemMetrics));
            }
        }

        private bool _isRefreshing;
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        private string _lastRefreshTime;
        public string LastRefreshTime
        {
            get => _lastRefreshTime;
            set
            {
                _lastRefreshTime = value;
                OnPropertyChanged(nameof(LastRefreshTime));
            }
        }

        #endregion

        /// <summary>
        /// Initialize the component UI
        /// </summary>
        private void InitializeComponent()
        {
            // Create main scroll viewer
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            // Create main stack panel
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(10)
            };

            // Add header
            var headerPanel = CreateHeaderPanel();
            mainPanel.Children.Add(headerPanel);

            // Add system metrics panel
            var metricsPanel = CreateSystemMetricsPanel();
            mainPanel.Children.Add(metricsPanel);

            // Add agents list
            var agentsPanel = CreateAgentsPanel();
            mainPanel.Children.Add(agentsPanel);

            scrollViewer.Content = mainPanel;
            this.Content = scrollViewer;
        }

        private StackPanel CreateHeaderPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Title
            var title = new TextBlock
            {
                Text = "A3sist Agent Status Monitor",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Refresh button
            var refreshButton = new Button
            {
                Content = "Refresh",
                Margin = new Thickness(10, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            refreshButton.Click += async (s, e) => await RefreshAgentStatusesAsync();

            // Last refresh time
            var lastRefreshLabel = new TextBlock
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            lastRefreshLabel.SetBinding(TextBlock.TextProperty, new Binding(nameof(LastRefreshTime)) { Source = this });

            panel.Children.Add(title);
            panel.Children.Add(refreshButton);
            panel.Children.Add(lastRefreshLabel);

            return panel;
        }

        private GroupBox CreateSystemMetricsPanel()
        {
            var groupBox = new GroupBox
            {
                Header = "System Metrics",
                Margin = new Thickness(0, 0, 0, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Total agents
            var totalAgentsPanel = new StackPanel();
            totalAgentsPanel.Children.Add(new TextBlock { Text = "Total Agents", FontWeight = FontWeights.Bold });
            var totalAgentsValue = new TextBlock();
            totalAgentsValue.SetBinding(TextBlock.TextProperty, new Binding("SystemMetrics.TotalAgents") { Source = this });
            totalAgentsPanel.Children.Add(totalAgentsValue);
            Grid.SetColumn(totalAgentsPanel, 0);

            // Active agents
            var activeAgentsPanel = new StackPanel();
            activeAgentsPanel.Children.Add(new TextBlock { Text = "Active Agents", FontWeight = FontWeights.Bold });
            var activeAgentsValue = new TextBlock();
            activeAgentsValue.SetBinding(TextBlock.TextProperty, new Binding("SystemMetrics.ActiveAgents") { Source = this });
            activeAgentsPanel.Children.Add(activeAgentsValue);
            Grid.SetColumn(activeAgentsPanel, 1);

            // System health
            var healthPanel = new StackPanel();
            healthPanel.Children.Add(new TextBlock { Text = "System Health", FontWeight = FontWeights.Bold });
            var healthValue = new TextBlock();
            healthValue.SetBinding(TextBlock.TextProperty, new Binding("SystemMetrics.OverallHealth") { Source = this });
            healthPanel.Children.Add(healthValue);
            Grid.SetColumn(healthPanel, 2);

            grid.Children.Add(totalAgentsPanel);
            grid.Children.Add(activeAgentsPanel);
            grid.Children.Add(healthPanel);

            groupBox.Content = grid;
            return groupBox;
        }

        private GroupBox CreateAgentsPanel()
        {
            var groupBox = new GroupBox
            {
                Header = "Agent Details"
            };

            var listView = new ListView();
            listView.SetBinding(ListView.ItemsSourceProperty, new Binding(nameof(AgentStatuses)) { Source = this });

            // Create data template for agent items
            var dataTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Grid));
            
            // Define columns
            factory.SetValue(Grid.MarginProperty, new Thickness(5));
            
            var nameColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            nameColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(2, GridUnitType.Star));
            factory.AppendChild(nameColumn);
            
            var typeColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            typeColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            factory.AppendChild(typeColumn);
            
            var statusColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            statusColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            factory.AppendChild(statusColumn);
            
            var healthColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            healthColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            factory.AppendChild(healthColumn);
            
            var metricsColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            metricsColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(2, GridUnitType.Star));
            factory.AppendChild(metricsColumn);

            // Name
            var nameText = new FrameworkElementFactory(typeof(TextBlock));
            nameText.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            nameText.SetValue(Grid.ColumnProperty, 0);
            factory.AppendChild(nameText);

            // Type
            var typeText = new FrameworkElementFactory(typeof(TextBlock));
            typeText.SetBinding(TextBlock.TextProperty, new Binding("Type"));
            typeText.SetValue(Grid.ColumnProperty, 1);
            factory.AppendChild(typeText);

            // Status
            var statusText = new FrameworkElementFactory(typeof(TextBlock));
            statusText.SetBinding(TextBlock.TextProperty, new Binding("Status"));
            statusText.SetBinding(TextBlock.ForegroundProperty, new Binding("StatusColor"));
            statusText.SetValue(Grid.ColumnProperty, 2);
            factory.AppendChild(statusText);

            // Health
            var healthText = new FrameworkElementFactory(typeof(TextBlock));
            healthText.SetBinding(TextBlock.TextProperty, new Binding("Health"));
            healthText.SetBinding(TextBlock.ForegroundProperty, new Binding("HealthColor"));
            healthText.SetValue(Grid.ColumnProperty, 3);
            factory.AppendChild(healthText);

            // Metrics
            var metricsText = new FrameworkElementFactory(typeof(TextBlock));
            metricsText.SetBinding(TextBlock.TextProperty, new Binding("MetricsDisplay"));
            metricsText.SetValue(Grid.ColumnProperty, 4);
            factory.AppendChild(metricsText);

            dataTemplate.VisualTree = factory;
            listView.ItemTemplate = dataTemplate;

            groupBox.Content = listView;
            return groupBox;
        }

        private async Task RefreshAgentStatusesAsync()
        {
            if (IsRefreshing) return;

            try
            {
                IsRefreshing = true;

                // TODO: Integrate with actual agent manager service
                // For now, simulate agent data
                await Task.Delay(500); // Simulate network delay

                AgentStatuses.Clear();

                var simulatedStatuses = new[]
                {
                    new AgentStatusDisplayModel
                    {
                        Name = "CSharpAgent",
                        Type = AgentType.Language,
                        Status = WorkStatus.Completed,
                        Health = HealthStatus.Healthy,
                        TasksProcessed = 45,
                        TasksSucceeded = 43,
                        TasksFailed = 2,
                        AverageProcessingTime = TimeSpan.FromSeconds(2.3),
                        LastActivity = DateTime.Now.AddMinutes(-2)
                    },
                    new AgentStatusDisplayModel
                    {
                        Name = "AnalyzerAgent",
                        Type = AgentType.Analyzer,
                        Status = WorkStatus.InProgress,
                        Health = HealthStatus.Healthy,
                        TasksProcessed = 23,
                        TasksSucceeded = 21,
                        TasksFailed = 2,
                        AverageProcessingTime = TimeSpan.FromSeconds(4.1),
                        LastActivity = DateTime.Now.AddSeconds(-30)
                    },
                    new AgentStatusDisplayModel
                    {
                        Name = "RefactorAgent",
                        Type = AgentType.Refactor,
                        Status = WorkStatus.Pending,
                        Health = HealthStatus.Warning,
                        TasksProcessed = 12,
                        TasksSucceeded = 10,
                        TasksFailed = 2,
                        AverageProcessingTime = TimeSpan.FromSeconds(6.7),
                        LastActivity = DateTime.Now.AddMinutes(-5)
                    },
                    new AgentStatusDisplayModel
                    {
                        Name = "DesignerAgent",
                        Type = AgentType.Designer,
                        Status = WorkStatus.Completed,
                        Health = HealthStatus.Healthy,
                        TasksProcessed = 8,
                        TasksSucceeded = 8,
                        TasksFailed = 0,
                        AverageProcessingTime = TimeSpan.FromSeconds(12.4),
                        LastActivity = DateTime.Now.AddMinutes(-1)
                    }
                };

                foreach (var status in simulatedStatuses)
                {
                    AgentStatuses.Add(status);
                }

                // Update system metrics
                SystemMetrics.TotalAgents = AgentStatuses.Count;
                SystemMetrics.ActiveAgents = AgentStatuses.Count(a => a.Status == WorkStatus.InProgress);
                SystemMetrics.OverallHealth = AgentStatuses.All(a => a.Health == HealthStatus.Healthy) ? "Healthy" :
                                            AgentStatuses.Any(a => a.Health == HealthStatus.Critical) ? "Critical" : "Warning";

                LastRefreshTime = $"Last updated: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                // TODO: Add proper logging
                LastRefreshTime = $"Error: {ex.Message}";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        #region Public Methods

        /// <summary>
        /// Refreshes agent statuses (exposed for testing)
        /// </summary>
        public async Task RefreshAgentStatusesPublicAsync()
        {
            await RefreshAgentStatusesAsync();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            _refreshTimer?.Stop();
            base.OnUnloaded(e);
        }
    }

    /// <summary>
    /// Display model for agent status with UI-specific properties
    /// </summary>
    public class AgentStatusDisplayModel : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private AgentType _type;
        public AgentType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(nameof(Type)); }
        }

        private WorkStatus _status;
        public WorkStatus Status
        {
            get => _status;
            set 
            { 
                _status = value; 
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        private HealthStatus _health;
        public HealthStatus Health
        {
            get => _health;
            set 
            { 
                _health = value; 
                OnPropertyChanged(nameof(Health));
                OnPropertyChanged(nameof(HealthColor));
            }
        }

        public int TasksProcessed { get; set; }
        public int TasksSucceeded { get; set; }
        public int TasksFailed { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public DateTime LastActivity { get; set; }

        public Brush StatusColor
        {
            get
            {
                return Status switch
                {
                    WorkStatus.InProgress => Brushes.Blue,
                    WorkStatus.Completed => Brushes.Green,
                    WorkStatus.Failed => Brushes.Red,
                    WorkStatus.Cancelled => Brushes.Orange,
                    WorkStatus.Paused => Brushes.Gray,
                    _ => Brushes.Black
                };
            }
        }

        public Brush HealthColor
        {
            get
            {
                return Health switch
                {
                    HealthStatus.Healthy => Brushes.Green,
                    HealthStatus.Warning => Brushes.Orange,
                    HealthStatus.Critical => Brushes.Red,
                    HealthStatus.Unhealthy => Brushes.DarkRed,
                    _ => Brushes.Gray
                };
            }
        }

        public string MetricsDisplay
        {
            get
            {
                var successRate = TasksProcessed > 0 ? (double)TasksSucceeded / TasksProcessed * 100 : 0;
                return $"Tasks: {TasksProcessed} | Success: {successRate:F1}% | Avg: {AverageProcessingTime.TotalSeconds:F1}s";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// System-wide metrics model
    /// </summary>
    public class SystemMetricsModel : INotifyPropertyChanged
    {
        private int _totalAgents;
        public int TotalAgents
        {
            get => _totalAgents;
            set { _totalAgents = value; OnPropertyChanged(nameof(TotalAgents)); }
        }

        private int _activeAgents;
        public int ActiveAgents
        {
            get => _activeAgents;
            set { _activeAgents = value; OnPropertyChanged(nameof(ActiveAgents)); }
        }

        private string _overallHealth;
        public string OverallHealth
        {
            get => _overallHealth;
            set { _overallHealth = value; OnPropertyChanged(nameof(OverallHealth)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}