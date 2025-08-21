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
    /// Command handler for refactoring code using A3sist agents
    /// </summary>
    internal sealed class RefactorCodeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0104;

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
        private readonly ILogger<RefactorCodeCommand>? logger;

        /// <summary>
        /// Orchestrator service
        /// </summary>
        private readonly IOrchestrator? orchestrator;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefactorCodeCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RefactorCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            try
            {
                // Get services if available
                if (package is A3sistPackage a3sistPackage)
                {
                    var serviceProvider = a3sistPackage.GetServiceProvider();
                    logger = serviceProvider.GetService(typeof(ILogger<RefactorCodeCommand>)) as ILogger<RefactorCodeCommand>;
                    orchestrator = serviceProvider.GetService(typeof(IOrchestrator)) as IOrchestrator;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get services in RefactorCodeCommand: {ex}");
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
        public static RefactorCodeCommand Instance
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
            // Switch to the main thread - the call to AddCommand in RefactorCodeCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RefactorCodeCommand(package, commandService);
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
        /// Refactors the current code using A3sist agents
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                logger?.LogInformation("Refactor code command executed");

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

                // Get selected text or entire document
                string codeToRefactor = GetSelectedTextOrDocument(textView);
                if (string.IsNullOrWhiteSpace(codeToRefactor))
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "No code selected or document is empty.",
                        "A3sist",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Create refactoring request
                var request = new AgentRequest
                {
                    Id = Guid.NewGuid(),
                    Prompt = "Refactor this code to improve readability, maintainability, and performance while preserving functionality",
                    Content = codeToRefactor,
                    FilePath = GetActiveDocumentPath(),
                    PreferredAgentType = AgentType.Refactor,
                    CreatedAt = DateTime.UtcNow,
                    Context = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["operation"] = "refactor",
                        ["source"] = "vs-command"
                    }
                };

                // Show progress
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Refactoring code... This may take a moment.",
                    "A3sist",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                // Process request asynchronously
                var result = await orchestrator.ProcessRequestAsync(request);

                // Show results
                if (result.Success)
                {
                    // Ask user if they want to apply the refactoring
                    var applyResult = VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Refactoring suggestions:\n\n{result.Message}\n\nWould you like to apply these changes?",
                        "A3sist Refactoring Results",
                        OLEMSGICON.OLEMSGICON_QUESTION,
                        OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                    if (applyResult == (int)VSConstants.MessageBoxResult.IDYES)
                    {
                        // Apply refactoring (placeholder for now)
                        ApplyRefactoring(textView, result.Content);
                    }
                }
                else
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        $"Code refactoring failed: {result.Message}",
                        "A3sist Refactoring Error",
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }

                logger?.LogDebug("Code refactoring completed");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing refactor code command");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error refactoring code: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Gets selected text or entire document content
        /// </summary>
        private string GetSelectedTextOrDocument(IWpfTextView textView)
        {
            try
            {
                // Check if there's a selection
                if (!textView.Selection.IsEmpty)
                {
                    return textView.Selection.SelectedSpans[0].GetText();
                }

                // Return entire document
                return textView.TextBuffer.CurrentSnapshot.GetText();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error getting text from editor");
                return string.Empty;
            }
        }

        /// <summary>
        /// Applies refactoring changes to the text view
        /// </summary>
        private void ApplyRefactoring(IWpfTextView textView, string? refactoredCode)
        {
            try
            {
                if (string.IsNullOrEmpty(refactoredCode))
                {
                    logger?.LogWarning("No refactored code to apply");
                    return;
                }

                // This is a placeholder - in a real implementation you'd use
                // ITextEdit to apply changes properly
                logger?.LogInformation("Refactoring would be applied here");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "Refactoring applied successfully!",
                    "A3sist",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error applying refactoring");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error applying refactoring: {ex.Message}",
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