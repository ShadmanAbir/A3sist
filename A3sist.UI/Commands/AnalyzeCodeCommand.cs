using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.Extensions.Logging;
using A3sist.Core.Services;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;

namespace A3sist.UI.Commands
{
    /// <summary>
    /// Command handler for analyzing code using A3sist agents
    /// </summary>
    internal sealed class AnalyzeCodeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0103;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("87654321-4321-4321-4321-210987654321");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Logger instance
        /// </summary>
        private readonly ILogger<AnalyzeCodeCommand>? logger;

        /// <summary>
        /// Orchestrator service
        /// </summary>
        private readonly IOrchestrator? orchestrator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyzeCodeCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private AnalyzeCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            try
            {
                // Get services if available
                if (package is A3sistPackage a3sistPackage)
                {
                    var serviceProvider = a3sistPackage.GetServiceProvider();
                    logger = serviceProvider.GetService(typeof(ILogger<AnalyzeCodeCommand>)) as ILogger<AnalyzeCodeCommand>;
                    orchestrator = serviceProvider.GetService(typeof(IOrchestrator)) as IOrchestrator;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get services in AnalyzeCodeCommand: {ex}");
            }

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            
            // Set up command visibility and enablement
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static AnalyzeCodeCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in AnalyzeCodeCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new AnalyzeCodeCommand(package, commandService);
        }

        /// <summary>
        /// Called before the command is displayed to determine visibility and enablement
        /// </summary>
        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (sender is OleMenuCommand command)
            {
                // Enable command only if there's an active text editor with content
                command.Enabled = GetActiveTextView() != null;
                command.Visible = true;
            }
        }

        /// <summary>
        /// Gets the currently active text view
        /// </summary>
        private IWpfTextView? GetActiveTextView()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // This is a simplified version - in a real implementation you'd use
                // IVsEditorAdaptersFactoryService to get the active text view
                return null; // Placeholder for now
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting active text view");
                return null;
            }
        }

        /// <summary>
        /// Analyzes the current code using A3sist agents
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                logger?.LogInformation("Analyze code command executed");

                if (orchestrator == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "A3sist orchestrator is not available. Please check the extension configuration.",
                        "A3sist Error",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Get current document content
                var textView = GetActiveTextView();
                if (textView == null)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "No active code editor found. Please open a code file and try again.",
                        "A3sist",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Create analysis request
                var request = new AgentRequest
                {
                    Id = Guid.NewGuid(),
                    Prompt = "Analyze this code for potential improvements, issues, and suggestions",
                    Content = textView.TextBuffer.CurrentSnapshot.GetText(),
                    FilePath = GetActiveDocumentPath(),
                    PreferredAgentType = AgentType.Designer,
                    CreatedAt = DateTime.UtcNow,
                    Context = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["operation"] = "analyze",
                        ["source"] = "vs-command"
                    }
                };

                // Show progress
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Analyzing code... This may take a moment.",
                    "A3sist",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                // Process request asynchronously
                var result = await orchestrator.ProcessRequestAsync(request);

                // Show results
                if (result.Success)
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Code analysis completed successfully:\n\n{result.Message}",
                        "A3sist Analysis Results",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Code analysis failed: {result.Message}",
                        "A3sist Analysis Error",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }

                logger?.LogDebug("Code analysis completed");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing analyze code command");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error analyzing code: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Gets the path of the currently active document
        /// </summary>
        private string? GetActiveDocumentPath()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // This is a placeholder - in a real implementation you'd use
                // IVsMonitorSelection to get the active document
                return null;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting active document path");
                return null;
            }
        }
    }
}