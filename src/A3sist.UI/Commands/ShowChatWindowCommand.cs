using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.Extensions.Logging;
using A3sist.UI.Services;
using A3sist.UI.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace A3sist.UI.Commands
{
    /// <summary>
    /// Command handler for showing the A3sist Chat tool window
    /// </summary>
    internal sealed class ShowChatWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0300; // Unique command ID for chat window

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789013");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Logger for this command
        /// </summary>
        private readonly ILogger<ShowChatWindowCommand> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowChatWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        /// <param name="logger">Logger instance</param>
        private ShowChatWindowCommand(AsyncPackage package, OleMenuCommandService commandService, ILogger<ShowChatWindowCommand> logger)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            _logger.LogDebug("ShowChatWindowCommand initialized");
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ShowChatWindowCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ShowChatWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            try
            {
                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                
                // Get logger from service provider
                var serviceProvider = (package as A3sistPackage)?.GetServiceProvider();
                var logger = serviceProvider?.GetService(typeof(ILogger<ShowChatWindowCommand>)) as ILogger<ShowChatWindowCommand>;
                
                if (logger == null)
                {
                    // Fallback logger
                    System.Diagnostics.Debug.WriteLine("Warning: Could not get logger for ShowChatWindowCommand");
                }

                Instance = new ShowChatWindowCommand(package, commandService, logger);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ShowChatWindowCommand: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger?.LogInformation("Showing A3sist Chat window");

                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exists it will be created.
                ToolWindowPane window = this.package.FindToolWindow(typeof(ChatToolWindow), 0, true);
                
                if (window?.Frame == null)
                {
                    _logger?.LogError("Cannot create A3sist Chat tool window");
                    throw new NotSupportedException("Cannot create A3sist Chat tool window");
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

                // Focus the chat input if possible
                if (window is ChatToolWindow chatWindow)
                {
                    try
                    {
                        chatWindow.ChatControl?.FocusInput();
                    }
                    catch (Exception focusEx)
                    {
                        _logger?.LogWarning(focusEx, "Could not focus chat input");
                    }
                }

                _logger?.LogDebug("A3sist Chat window shown successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing A3sist Chat window");
                
                // Show error message to user
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Failed to show A3sist Chat window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}