using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Service for integrating with Visual Studio status bar
    /// </summary>
    public class StatusBarIntegration
    {
        private IVsStatusbar? _statusBar;

        public StatusBarIntegration()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
            }
            catch (Exception)
            {
                // Status bar integration is optional
                _statusBar = null;
            }
        }

        public void ShowMessage(string message)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _statusBar?.SetText($"A3sist: {message}");
            }
            catch (Exception)
            {
                // Silently fail - status bar updates are not critical
            }
        }

        public void ShowProgress(string message, uint completed, uint total)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _statusBar?.Progress(ref completed, ref total, message, completed, total);
            }
            catch (Exception)
            {
                // Silently fail
            }
        }

        public void ClearMessage()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _statusBar?.Clear();
            }
            catch (Exception)
            {
                // Silently fail
            }
        }
    }
}