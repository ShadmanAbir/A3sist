using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Extensions.Logging;
using A3sist.Core.Services;
using A3sist.UI.ToolWindows;

namespace A3sist.UI.Commands
{
    /// <summary>
    /// Main A3sist command that provides access to the extension's functionality
    /// </summary>
    internal sealed class A3sistMainCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

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
        private readonly ILogger<A3sistMainCommand>? logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="A3sistMainCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private A3sistMainCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            try
            {
                // Get logger if available
                if (package is A3sistPackage a3sistPackage)
                {
                    var serviceProvider = a3sistPackage.GetServiceProvider();
                    logger = serviceProvider.GetService(typeof(ILogger<A3sistMainCommand>)) as ILogger<A3sistMainCommand>;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get logger in A3sistMainCommand: {ex}");
            }

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static A3sistMainCommand Instance
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
            // Switch to the main thread - the call to AddCommand in A3sistMainCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new A3sistMainCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                logger?.LogInformation("A3sist main command executed");

                // Show the main A3sist tool window
                ShowA3ToolWindowCommand.Instance?.Execute(sender, e);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error executing A3sist main command");
                
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error executing A3sist command: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}