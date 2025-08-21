using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using A3sist.UI.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace A3sist.UI.Components
{
    /// <summary>
    /// Integrates A3sist progress and status information with Visual Studio's status bar
    /// </summary>
    public class StatusBarIntegration : IDisposable
    {
        private readonly IVsStatusbar _statusBar;
        private readonly ProgressNotificationService _notificationService;
        private readonly DispatcherTimer _updateTimer;
        private bool _disposed;
        private uint _progressCookie;
        private bool _isProgressActive;

        public StatusBarIntegration()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            _statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
            _notificationService = ProgressNotificationService.Instance;
            
            // Set up timer for periodic updates
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _updateTimer.Tick += OnUpdateTimer;
            
            // Subscribe to notification service events
            _notificationService.PropertyChanged += OnNotificationServicePropertyChanged;
            
            // Start monitoring
            _updateTimer.Start();
        }

        private void OnNotificationServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                switch (e.PropertyName)
                {
                    case nameof(ProgressNotificationService.HasActiveOperations):
                        UpdateProgressState();
                        break;
                    case nameof(ProgressNotificationService.CurrentOperationText):
                        UpdateStatusText();
                        break;
                    case nameof(ProgressNotificationService.OverallProgress):
                        UpdateProgressValue();
                        break;
                }
            });
        }

        private void OnUpdateTimer(object sender, EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                UpdateStatusBar();
            });
        }

        private void UpdateStatusBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (_statusBar == null) return;

            try
            {
                UpdateProgressState();
                UpdateStatusText();
                UpdateProgressValue();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid disrupting VS
                System.Diagnostics.Debug.WriteLine($"Error updating status bar: {ex}");
            }
        }

        private void UpdateProgressState()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (_statusBar == null) return;

            var hasActiveOperations = _notificationService.HasActiveOperations;
            
            if (hasActiveOperations && !_isProgressActive)
            {
                // Start progress
                _statusBar.Progress(ref _progressCookie, 1, "", 0, 0);
                _isProgressActive = true;
            }
            else if (!hasActiveOperations && _isProgressActive)
            {
                // End progress
                _statusBar.Progress(ref _progressCookie, 0, "", 0, 0);
                _isProgressActive = false;
            }
        }

        private void UpdateStatusText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (_statusBar == null) return;

            var statusText = _notificationService.HasActiveOperations 
                ? $"A3sist: {_notificationService.CurrentOperationText}"
                : "A3sist: Ready";

            _statusBar.SetText(statusText);
        }

        private void UpdateProgressValue()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (_statusBar == null || !_isProgressActive) return;

            var progress = (uint)_notificationService.OverallProgress;
            _statusBar.Progress(ref _progressCookie, 1, "", progress, 100);
        }

        /// <summary>
        /// Shows a message in the status bar
        /// </summary>
        public void ShowMessage(string message, bool isError = false)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if (_statusBar != null)
                {
                    var fullMessage = $"A3sist: {message}";
                    _statusBar.SetText(fullMessage);
                    
                    if (isError)
                    {
                        // Flash the status bar for errors
                        _statusBar.Animation(1, ref _progressCookie);
                        
                        // Stop animation after a delay
                        var timer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromSeconds(3)
                        };
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                            {
                                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                                _statusBar?.Animation(0, ref _progressCookie);
                            });
                        };
                        timer.Start();
                    }
                }
            });
        }

        /// <summary>
        /// Clears the status bar message
        /// </summary>
        public void ClearMessage()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if (_statusBar != null)
                {
                    _statusBar.Clear();
                }
            });
        }

        /// <summary>
        /// Shows progress for a specific operation
        /// </summary>
        public void ShowProgress(string label, uint completed, uint total)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if (_statusBar != null)
                {
                    var fullLabel = $"A3sist: {label}";
                    _statusBar.Progress(ref _progressCookie, 1, fullLabel, completed, total);
                }
            });
        }

        /// <summary>
        /// Hides progress indicator
        /// </summary>
        public void HideProgress()
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if (_statusBar != null)
                {
                    _statusBar.Progress(ref _progressCookie, 0, "", 0, 0);
                    _isProgressActive = false;
                }
            });
        }

        public void Dispose()
        {
            if (_disposed) return;

            _updateTimer?.Stop();
            
            if (_notificationService != null)
            {
                _notificationService.PropertyChanged -= OnNotificationServicePropertyChanged;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                
                if (_statusBar != null && _isProgressActive)
                {
                    _statusBar.Progress(ref _progressCookie, 0, "", 0, 0);
                    _statusBar.Clear();
                }
            });

            _disposed = true;
        }
    }
}