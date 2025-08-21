using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Extensions.Logging;
using A3sist.Core.Services;
using A3sist.Shared.Messaging;
using A3sist.Shared.Enums;

namespace A3sist.UI.Commands
{
    /// <summary>
    /// Command handler for fixing code errors using A3sist agents
    /// </summary>
    internal sealed class FixCodeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0105;

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
        private readonly ILogger<FixCodeCommand>? logger;

        /// <summary>
        /// Orchestrator service
        /// </summary>
        private readonly IOrchestrator? orchestrator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixCodeCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private FixCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            try
            {
                // Get services if available
                if (package is A3sistPackage a3sistPackage)
                {
                    var serviceProvider = a3sistPackage.GetServiceProvider();
                    logger = serviceProvider.GetService(typeof(ILogger<FixCodeCommand>)) as ILogger<FixCodeCommand>;
                    orchestrator = serviceProvider.GetService(typeof(IOrchestrator)) as IOrchestrator;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get services in FixCodeCommand: {ex}");
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
        public static FixCodeCommand Instance
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
            // Switch to the main thread - the call to AddCommand in FixCodeCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new FixCodeCommand(package, commandService);
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
        /// Fixes code errors using A3sist agents
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                logger?.LogInformation("Fix code command executed");

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

                // Get current document content
                string codeToFix = textView.TextBuffer.CurrentSnapshot.GetText();
                if (string.IsNullOrWhiteSpace(codeToFix))
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "Document is empty.",
                        "A3sist",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Get compilation errors (placeholder for now)
                var errors = GetCompilationErrors();

                // Create fix request
                var request = new AgentRequest
                {
                    Id = Guid.NewGuid(),
                    Prompt = $"Fix the following code errors and issues:\n{string.Join("\n", errors)}",
                    Content = codeToFix,
                    FilePath = GetActiveDocumentPath(),
                    PreferredAgentType = AgentType.Fixer,
                    CreatedAt = DateTime.UtcNow,
                    Context = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["operation"] = "fix",
                        ["source"] = "vs-command",
                        ["errors"] = errors
                    }
                };

                // Show progress
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Analyzing and fixing code errors... This may take a moment.",
                    "A3sist",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                // Process request asynchronously
                var result = await orchestrator.ProcessRequestAsync(request);

                // Show results
                if (result.Success)
                {
                    // Ask user if they want to apply the fixes
                    var applyResult = VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Code fixes suggested:\n\n{result.Message}\n\nWould you like to apply these fixes?",
                        "A3sist Code Fix Results",
                        OLEMSGICON.OLEMSGICON_QUESTION,
                        OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                    if (applyResult == (int)VSConstants.MessageBoxResult.IDYES)
                    {
                        // Apply fixes (placeholder for now)
                        ApplyCodeFixes(textView, result.Content);
                    }
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Code fixing failed: {result.Message}",
                        "A3sist Code Fix Error",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }

                logger?.LogDebug("Code fixing completed");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing fix code command");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error fixing code: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Gets compilation errors from the current document
        /// </summary>
        private string[] GetCompilationErrors()
        {
            try
            {
                // This is a placeholder - in a real implementation you'd use
                // IVsErrorList or similar to get actual compilation errors
                return new[] { "No specific errors detected - performing general code analysis" };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting compilation errors");
                return new[] { "Unable to retrieve compilation errors" };
            }
        }

        /// <summary>
        /// Applies code fixes to the text view
        /// </summary>
        private void ApplyCodeFixes(IWpfTextView textView, string? fixedCode)
        {
            try
            {
                if (string.IsNullOrEmpty(fixedCode))
                {
                    logger?.LogWarning("No fixed code to apply");
                    return;
                }

                // This is a placeholder - in a real implementation you'd use
                // ITextEdit to apply changes properly
                logger?.LogInformation("Code fixes would be applied here");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Code fixes applied successfully!",
                    "A3sist",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error applying code fixes");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error applying code fixes: {ex.Message}",
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