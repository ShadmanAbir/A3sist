using A3sist.UI.Models.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;

namespace A3sist.UI.Services.Chat
{
    /// <summary>
    /// Service for gathering Visual Studio context information
    /// </summary>
    public interface IContextService
    {
        Task<ChatContext> GetCurrentContextAsync();
        Task<string?> GetSelectedTextAsync();
        Task<string?> GetCurrentFilePathAsync();
        Task<string?> GetCurrentProjectPathAsync();
        Task<List<string>> GetOpenFilesAsync();
        Task<List<CompilerError>> GetCurrentErrorsAsync();
    }

    /// <summary>
    /// Implementation of context service that integrates with Visual Studio services
    /// </summary>
    public class ContextService : IContextService
    {
        private readonly ILogger<ContextService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ContextService(ILogger<ContextService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets comprehensive context from Visual Studio
        /// </summary>
        public async Task<ChatContext> GetCurrentContextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var context = new ChatContext
                {
                    CurrentFile = await GetCurrentFilePathAsync(),
                    SelectedText = await GetSelectedTextAsync(),
                    ProjectPath = await GetCurrentProjectPathAsync(),
                    OpenFiles = await GetOpenFilesAsync(),
                    Errors = await GetCurrentErrorsAsync(),
                    EnvironmentInfo = await GetEnvironmentInfoAsync()
                };

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current Visual Studio context");
                return new ChatContext();
            }
        }

        /// <summary>
        /// Gets currently selected text in the active editor
        /// </summary>
        public async Task<string?> GetSelectedTextAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
                if (dte?.ActiveDocument?.Selection is TextSelection selection)
                {
                    return selection.Text;
                }

                // Alternative method using text manager
                var textManager = _serviceProvider.GetService(typeof(SVsTextManager)) as IVsTextManager2;
                if (textManager != null)
                {
                    if (textManager.GetActiveView2(1, null, (uint)_VIEWFRAMETYPE.vftCodeWindow, out var textView) == VSConstants.S_OK)
                    {
                        if (textView.GetSelection(out int startLine, out int startCol, out int endLine, out int endCol) == VSConstants.S_OK)
                        {
                            if (textView.GetTextStream(startLine, startCol, endLine, endCol, out string selectedText) == VSConstants.S_OK)
                            {
                                return selectedText;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected text");
            }

            return null;
        }

        /// <summary>
        /// Gets the path of the currently active file
        /// </summary>
        public async Task<string?> GetCurrentFilePathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
                return dte?.ActiveDocument?.FullName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current file path");
                return null;
            }
        }

        /// <summary>
        /// Gets the path of the current project
        /// </summary>
        public async Task<string?> GetCurrentProjectPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
                var activeProject = dte?.ActiveDocument?.ProjectItem?.ContainingProject;
                
                if (activeProject != null)
                {
                    return Path.GetDirectoryName(activeProject.FullName);
                }

                // Fallback to solution directory
                if (dte?.Solution != null && !string.IsNullOrEmpty(dte.Solution.FullName))
                {
                    return Path.GetDirectoryName(dte.Solution.FullName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current project path");
            }

            return null;
        }

        /// <summary>
        /// Gets list of currently open files
        /// </summary>
        public async Task<List<string>> GetOpenFilesAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var openFiles = new List<string>();

            try
            {
                var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
                if (dte?.Documents != null)
                {
                    foreach (Document document in dte.Documents)
                    {
                        if (!string.IsNullOrEmpty(document.FullName))
                        {
                            openFiles.Add(document.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open files");
            }

            return openFiles;
        }

        /// <summary>
        /// Gets current compilation errors and warnings
        /// </summary>
        public async Task<List<CompilerError>> GetCurrentErrorsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var errors = new List<CompilerError>();

            try
            {
                var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
                if (dte?.ToolWindows?.ErrorList != null)
                {
                    var errorItems = dte.ToolWindows.ErrorList.ErrorItems;
                    
                    for (int i = 1; i <= errorItems.Count; i++)
                    {
                        var errorItem = errorItems.Item(i);
                        
                        errors.Add(new CompilerError
                        {
                            File = errorItem.FileName,
                            Line = errorItem.Line,
                            Column = errorItem.Column,
                            Message = errorItem.Description,
                            Severity = GetSeverityString(errorItem),
                            Code = GetErrorCode(errorItem)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current errors");
            }

            return errors;
        }

        /// <summary>
        /// Gets environment information about Visual Studio and the system
        /// </summary>
        private async Task<Dictionary<string, object>> GetEnvironmentInfoAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var info = new Dictionary<string, object>();

            try
            {
                var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
                
                if (dte != null)
                {
                    info["VisualStudioVersion"] = dte.Version;
                    info["VisualStudioEdition"] = dte.Edition;
                    
                    if (dte.Solution != null)
                    {
                        info["SolutionName"] = Path.GetFileNameWithoutExtension(dte.Solution.FullName);
                        info["ProjectCount"] = dte.Solution.Projects.Count;
                    }

                    // Get active configuration
                    if (dte.Solution?.SolutionBuild?.ActiveConfiguration != null)
                    {
                        var config = dte.Solution.SolutionBuild.ActiveConfiguration;
                        info["ActiveConfiguration"] = config.Name;
                        info["ActivePlatform"] = config.PlatformName;
                    }
                }

                // System information
                info["OSVersion"] = Environment.OSVersion.ToString();
                info["ProcessorCount"] = Environment.ProcessorCount;
                info["WorkingSet"] = Environment.WorkingSet;
                info["CLRVersion"] = Environment.Version.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting environment information");
            }

            return info;
        }

        private string GetSeverityString(ErrorItem errorItem)
        {
            try
            {
                // Map Visual Studio error types to severity strings
                return "Error"; // Simplified - would need proper mapping
            }
            catch
            {
                return "Unknown";
            }
        }

        private string GetErrorCode(ErrorItem errorItem)
        {
            try
            {
                // Extract error code if available
                var description = errorItem.Description ?? "";
                var codeStart = description.IndexOf('(');
                var codeEnd = description.IndexOf(')', codeStart + 1);
                
                if (codeStart >= 0 && codeEnd > codeStart)
                {
                    return description.Substring(codeStart + 1, codeEnd - codeStart - 1);
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}