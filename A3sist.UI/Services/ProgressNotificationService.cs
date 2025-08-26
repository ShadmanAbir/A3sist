using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Service for managing progress indicators and notifications in the UI
    /// </summary>
    public class ProgressNotificationService : INotifyPropertyChanged
    {
        private static ProgressNotificationService _instance;
        private readonly Dispatcher _dispatcher;

        public static ProgressNotificationService Instance => _instance ??= new ProgressNotificationService();

        private ProgressNotificationService()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            ActiveOperations = new ObservableCollection<ProgressOperation>();
            Notifications = new ObservableCollection<NotificationItem>();
        }

        #region Properties

        public ObservableCollection<ProgressOperation> ActiveOperations { get; }
        public ObservableCollection<NotificationItem> Notifications { get; }

        private bool _hasActiveOperations;
        public bool HasActiveOperations
        {
            get => _hasActiveOperations;
            private set
            {
                _hasActiveOperations = value;
                OnPropertyChanged(nameof(HasActiveOperations));
            }
        }

        private string _currentOperationText;
        public string CurrentOperationText
        {
            get => _currentOperationText;
            private set
            {
                _currentOperationText = value;
                OnPropertyChanged(nameof(CurrentOperationText));
            }
        }

        private double _overallProgress;
        public double OverallProgress
        {
            get => _overallProgress;
            private set
            {
                _overallProgress = value;
                OnPropertyChanged(nameof(OverallProgress));
            }
        }

        #endregion

        #region Progress Operations

        /// <summary>
        /// Starts a new progress operation
        /// </summary>
        public ProgressOperation StartOperation(string description, bool isIndeterminate = false)
        {
            var operation = new ProgressOperation(description, isIndeterminate);
            operation.Completed += OnOperationCompleted;
            operation.ProgressChanged += OnOperationProgressChanged;

            _dispatcher.Invoke(() =>
            {
                ActiveOperations.Add(operation);
                UpdateOverallProgress();
            });

            return operation;
        }

        private void OnOperationCompleted(object sender, EventArgs e)
        {
            if (sender is ProgressOperation operation)
            {
                _dispatcher.Invoke(() =>
                {
                    ActiveOperations.Remove(operation);
                    UpdateOverallProgress();

                    // Add completion notification
                    var notification = new NotificationItem
                    {
                        Title = "Operation Completed",
                        Message = operation.Description,
                        Type = operation.IsSuccess ? NotificationType.Success : NotificationType.Error,
                        Timestamp = DateTime.Now
                    };
                    AddNotification(notification);
                });
            }
        }

        private void OnOperationProgressChanged(object sender, EventArgs e)
        {
            _dispatcher.Invoke(UpdateOverallProgress);
        }

        private void UpdateOverallProgress()
        {
            HasActiveOperations = ActiveOperations.Count > 0;

            if (!HasActiveOperations)
            {
                CurrentOperationText = "Ready";
                OverallProgress = 0;
                return;
            }

            // Calculate overall progress
            double totalProgress = 0;
            int determinateOperations = 0;
            string currentOperation = null;

            foreach (var operation in ActiveOperations)
            {
                if (!operation.IsIndeterminate)
                {
                    totalProgress += operation.Progress;
                    determinateOperations++;
                }

                if (currentOperation == null || operation.IsActive)
                {
                    currentOperation = operation.Description;
                }
            }

            OverallProgress = determinateOperations > 0 ? totalProgress / determinateOperations : 0;
            CurrentOperationText = currentOperation ?? "Processing...";
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Adds a notification to the system
        /// </summary>
        public void AddNotification(NotificationItem notification)
        {
            _dispatcher.Invoke(() =>
            {
                Notifications.Insert(0, notification);

                // Auto-remove after delay for non-error notifications
                if (notification.Type != NotificationType.Error)
                {
                    Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                    {
                        _dispatcher.Invoke(() => Notifications.Remove(notification));
                    });
                }

                // Keep only last 50 notifications
                while (Notifications.Count > 50)
                {
                    Notifications.RemoveAt(Notifications.Count - 1);
                }
            });
        }

        /// <summary>
        /// Shows a success notification
        /// </summary>
        public void ShowSuccess(string title, string message = null)
        {
            AddNotification(new NotificationItem
            {
                Title = title,
                Message = message,
                Type = NotificationType.Success,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Shows an error notification
        /// </summary>
        public void ShowError(string title, string message = null)
        {
            AddNotification(new NotificationItem
            {
                Title = title,
                Message = message,
                Type = NotificationType.Error,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Shows an information notification
        /// </summary>
        public void ShowInfo(string title, string message = null)
        {
            AddNotification(new NotificationItem
            {
                Title = title,
                Message = message,
                Type = NotificationType.Info,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        public void ShowWarning(string title, string message = null)
        {
            AddNotification(new NotificationItem
            {
                Title = title,
                Message = message,
                Type = NotificationType.Warning,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Clears all notifications
        /// </summary>
        public void ClearNotifications()
        {
            _dispatcher.Invoke(() => Notifications.Clear());
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Represents a progress operation
    /// </summary>
    public class ProgressOperation : INotifyPropertyChanged
    {
        private double _progress;
        private bool _isActive = true;
        private bool _isSuccess = true;
        private string _statusMessage;

        public ProgressOperation(string description, bool isIndeterminate = false)
        {
            Description = description;
            IsIndeterminate = isIndeterminate;
            StartTime = DateTime.Now;
        }

        public string Description { get; }
        public bool IsIndeterminate { get; }
        public DateTime StartTime { get; }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = Math.Max(0, Math.Min(100, value));
                OnPropertyChanged(nameof(Progress));
                ProgressChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsActive
        {
            get => _isActive;
            private set
            {
                _isActive = value;
                OnPropertyChanged(nameof(IsActive));
            }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            private set
            {
                _isSuccess = value;
                OnPropertyChanged(nameof(IsSuccess));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public TimeSpan Duration => DateTime.Now - StartTime;

        public event EventHandler Completed;
        public event EventHandler ProgressChanged;

        /// <summary>
        /// Completes the operation successfully
        /// </summary>
        public void Complete(string statusMessage = null)
        {
            IsActive = false;
            IsSuccess = true;
            StatusMessage = statusMessage ?? "Completed";
            Progress = 100;
            Completed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fails the operation
        /// </summary>
        public void Fail(string errorMessage)
        {
            IsActive = false;
            IsSuccess = false;
            StatusMessage = errorMessage;
            Completed?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a notification item
    /// </summary>
    public class NotificationItem : INotifyPropertyChanged
    {
        private bool _isRead;

        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsRead
        {
            get => _isRead;
            set
            {
                _isRead = value;
                OnPropertyChanged(nameof(IsRead));
            }
        }

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - Timestamp;
                return timeSpan.TotalMinutes < 1 ? "Just now" :
                       timeSpan.TotalHours < 1 ? $"{(int)timeSpan.TotalMinutes}m ago" :
                       timeSpan.TotalDays < 1 ? $"{(int)timeSpan.TotalHours}h ago" :
                       $"{(int)timeSpan.TotalDays}d ago";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Types of notifications
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}