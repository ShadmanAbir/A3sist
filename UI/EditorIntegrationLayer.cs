using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace A3sist.UI
{
    public class EditorIntegrationLayer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EditorIntegrationLayer> _logger;

        public EditorIntegrationLayer(IServiceProvider serviceProvider, ILogger<EditorIntegrationLayer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task OpenFileInEditor(string path)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                var window = dte.ItemOperations.OpenFile(path);

                if (window != null)
                {
                    _logger.LogInformation($"Successfully opened file in editor: {path}");
                }
                else
                {
                    _logger.LogWarning($"Failed to open file in editor: {path}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening file in editor: {path}");
                throw;
            }
        }

        public async Task RefreshEditorView(string path)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                var document = dte.Documents.Item(path);

                if (document != null)
                {
                    document.Activate();
                    document.Save();
                    _logger.LogInformation($"Refreshed editor view for file: {path}");
                }
                else
                {
                    _logger.LogWarning($"File not found in editor: {path}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error refreshing editor view for file: {path}");
                throw;
            }
        }

        public async Task ShowDiffView(string originalPath, string modifiedPath)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
                var diffTool = dte.Commands.Item("Tools.DiffFiles");

                if (diffTool != null)
                {
                    dte.ExecuteCommand("Tools.DiffFiles", $"{originalPath} {modifiedPath}");
                    _logger.LogInformation($"Showing diff between {originalPath} and {modifiedPath}");
                }
                else
                {
                    _logger.LogWarning("Diff tool not available in current environment");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing diff view for files: {originalPath} and {modifiedPath}");
                throw;
            }
        }
    }
}