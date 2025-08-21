using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Extensions.Logging;
using A3sist.UI.ToolWindows;

namespace A3sist.UI.Commands
{
    /// <summary>
    /// Command handler for showing the agent status monitoring window
    /// </summary>
    internal sealed class ShowAgentStatusCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0102;

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
        private readonly ILogger<ShowAgentStatusCommand>? logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowAgentStatusCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ShowAgentStatusCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            try
            {
                // Get logger if available
                if (package is A3sistPackage a3sistPackage)
                {
                    var serviceProvider = a3sistPackage.GetServiceProvider();
                    logger = serviceProvider.GetService(typeof(ILogger<ShowAgentStatusCommand>)) as ILogger<ShowAgentStatusCommand>;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get logger in ShowAgentStatusCommand: {ex}");
            }

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ShowAgentStatusCommand Instance
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
            // Switch to the main thread - the call to AddCommand in ShowAgentStatusCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ShowAgentStatusCommand(package, commandService);
        }

        /// <summary>
        /// Shows the agent status tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                logger?.LogInformation("Show agent status command executed");

                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exists it will be created.
                ToolWindowPane window = this.package.FindToolWindow(typeof(AgentStatusWindow), 0, true);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create agent status tool window");
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

                logger?.LogDebug("Agent status tool window shown successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error showing agent status tool window");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error showing agent status window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}