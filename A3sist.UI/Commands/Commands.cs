using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using A3sist.UI.UI;
using A3sist.UI.Services;

namespace A3sist.UI.Commands
{
    /// <summary>
    /// Command handler for A3sist extension commands
    /// </summary>
    internal sealed class Commands
    {
        /// <summary>
        /// Command IDs defined in the .vsct file
        /// </summary>
        public const int ShowA3sistToolWindowCommandId = 0x0100;
        public const int OpenChatWindowCommandId = 0x0101;
        public const int OpenConfigurationCommandId = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("74A89A6E-00F3-4C44-9854-2C4BF7F9C3F0");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Commands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Commands(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            // Show A3sist Tool Window Command
            var showToolWindowCommandID = new CommandID(CommandSet, ShowA3sistToolWindowCommandId);
            var showToolWindowCommand = new MenuCommand(this.ShowA3sistToolWindow, showToolWindowCommandID);
            commandService.AddCommand(showToolWindowCommand);

            // Open Chat Window Command
            var openChatCommandID = new CommandID(CommandSet, OpenChatWindowCommandId);
            var openChatCommand = new MenuCommand(this.OpenChatWindow, openChatCommandID);
            commandService.AddCommand(openChatCommand);

            // Open Configuration Command
            var openConfigCommandID = new CommandID(CommandSet, OpenConfigurationCommandId);
            var openConfigCommand = new MenuCommand(this.OpenConfiguration, openConfigCommandID);
            commandService.AddCommand(openConfigCommand);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Commands Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Commands's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Commands(package, commandService);
        }

        /// <summary>
        /// Shows the A3sist tool window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void ShowA3sistToolWindow(object sender, EventArgs e)
        {
            _ = this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                ToolWindowPane window = await this.package.ShowToolWindowAsync(typeof(A3sistToolWindowPane), 0, true, this.package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create A3sist tool window");
                }
            });
        }

        /// <summary>
        /// Opens the chat window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OpenChatWindow(object sender, EventArgs e)
        {
            try
            {
                var package = this.package as A3sistPackage;
                if (package != null)
                {
                    var apiClient = package.GetService<IA3sistApiClient>();
                    var configService = package.GetService<IA3sistConfigurationService>();

                    if (apiClient != null && configService != null)
                    {
                        var chatWindow = new ChatWindow(apiClient, configService);
                        chatWindow.Show();
                    }
                    else
                    {
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            "A3sist services are not available. Please restart Visual Studio.",
                            "A3sist Error",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Failed to open chat window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Opens the configuration window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OpenConfiguration(object sender, EventArgs e)
        {
            try
            {
                var package = this.package as A3sistPackage;
                if (package != null)
                {
                    var apiClient = package.GetService<IA3sistApiClient>();
                    var configService = package.GetService<IA3sistConfigurationService>();

                    if (apiClient != null && configService != null)
                    {
                        var configWindow = new ConfigurationWindow(apiClient, configService);
                        configWindow.ShowDialog();
                    }
                    else
                    {
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            "A3sist services are not available. Please restart Visual Studio.",
                            "A3sist Error",
                            OLEMSGICON.OLEMSGICON_WARNING,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    }
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Failed to open configuration window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}