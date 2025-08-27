using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using A3sist.UI;
using A3sist.Services;
using Task = System.Threading.Tasks.Task;

namespace A3sist.Commands
{
    /// <summary>
    /// Command handler for opening the A3sist chat window
    /// </summary>
    internal sealed class OpenChatCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789123");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenChatCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private OpenChatCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenChatCommand Instance
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
            // Switch to the main thread - the call to AddCommand in OpenChatCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new OpenChatCommand(package, commandService);
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
                // Get services from package
                if (package is A3sistPackage a3sistPackage)
                {
                    var chatService = a3sistPackage.GetService<IChatService>();
                    var modelService = a3sistPackage.GetService<IModelManagementService>();
                    var configService = a3sistPackage.GetService<IA3sistConfigurationService>();

                    // Create and show chat window
                    var chatWindow = new ChatWindow(chatService, modelService, configService);
                    chatWindow.Show();
                }
            }
            catch (Exception ex)
            {
                // Show error message
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error opening A3sist chat: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static void Initialize(A3sistPackage package, OleMenuCommandService commandService)
        {
            Instance = new OpenChatCommand(package, commandService);
        }
    }

    /// <summary>
    /// Command handler for opening the A3sist configuration window
    /// </summary>
    internal sealed class ConfigureA3sistCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789123");
        private readonly AsyncPackage package;

        private ConfigureA3sistCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ConfigureA3sistCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ConfigureA3sistCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (package is A3sistPackage a3sistPackage)
                {
                    var configService = a3sistPackage.GetService<IA3sistConfigurationService>();
                    var modelService = a3sistPackage.GetService<IModelManagementService>();
                    var mcpService = a3sistPackage.GetService<IMCPClientService>();
                    var ragService = a3sistPackage.GetService<IRAGEngineService>();

                    var configWindow = new ConfigurationWindow(configService, modelService, mcpService, ragService);
                    configWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error opening A3sist configuration: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static void Initialize(A3sistPackage package, OleMenuCommandService commandService)
        {
            Instance = new ConfigureA3sistCommand(package, commandService);
        }
    }

    /// <summary>
    /// Command handler for toggling autocomplete functionality
    /// </summary>
    internal sealed class ToggleAutoCompleteCommand
    {
        public const int CommandId = 0x0102;
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789123");
        private readonly AsyncPackage package;

        private ToggleAutoCompleteCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += UpdateCommandStatus;
            commandService.AddCommand(menuItem);
        }

        public static ToggleAutoCompleteCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ToggleAutoCompleteCommand(package, commandService);
        }

        private async void UpdateCommandStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand command && package is A3sistPackage a3sistPackage)
            {
                try
                {
                    var autoCompleteService = a3sistPackage.GetService<IAutoCompleteService>();
                    var isEnabled = await autoCompleteService.IsAutoCompleteEnabledAsync();
                    command.Checked = isEnabled;
                    command.Text = isEnabled ? "Disable A3sist AutoComplete" : "Enable A3sist AutoComplete";
                }
                catch
                {
                    command.Visible = false;
                }
            }
        }

        private async void Execute(object sender, EventArgs e)
        {
            try
            {
                if (package is A3sistPackage a3sistPackage)
                {
                    var autoCompleteService = a3sistPackage.GetService<IAutoCompleteService>();
                    var currentState = await autoCompleteService.IsAutoCompleteEnabledAsync();
                    await autoCompleteService.SetAutoCompleteEnabledAsync(!currentState);

                    var statusMessage = !currentState ? "A3sist AutoComplete enabled" : "A3sist AutoComplete disabled";
                    
                    // Show status in VS status bar
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var statusBar = await ServiceProvider.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                    statusBar?.SetText(statusMessage);
                }
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error toggling autocomplete: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static void Initialize(A3sistPackage package, OleMenuCommandService commandService)
        {
            Instance = new ToggleAutoCompleteCommand(package, commandService);
        }
    }

    /// <summary>
    /// Command handler for refactoring code
    /// </summary>
    internal sealed class RefactorCodeCommand
    {
        public const int CommandId = 0x0103;
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789123");
        private readonly AsyncPackage package;

        private RefactorCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static RefactorCodeCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RefactorCodeCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                if (package is A3sistPackage a3sistPackage)
                {
                    var refactoringService = a3sistPackage.GetService<IRefactoringService>();
                    var codeAnalysisService = a3sistPackage.GetService<ICodeAnalysisService>();

                    // Get active document
                    var dte = await ServiceProvider.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                    var activeDocument = dte?.ActiveDocument;

                    if (activeDocument?.Selection is EnvDTE.TextSelection selection)
                    {
                        var selectedText = selection.Text;
                        var language = await codeAnalysisService.DetectLanguageAsync(selectedText, activeDocument.Name);
                        
                        if (!string.IsNullOrEmpty(selectedText))
                        {
                            var suggestions = await refactoringService.GetRefactoringSuggestionsAsync(selectedText, language);
                            
                            if (suggestions.Any())
                            {
                                var refactorWindow = new RefactoringWindow(suggestions, refactoringService);
                                refactorWindow.ShowDialog();
                            }
                            else
                            {
                                VsShellUtilities.ShowMessageBox(
                                    this.package,
                                    "No refactoring suggestions available for the selected code.",
                                    "A3sist Refactoring",
                                    OLEMSGICON.OLEMSGICON_INFO,
                                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                            }
                        }
                        else
                        {
                            VsShellUtilities.ShowMessageBox(
                                this.package,
                                "Please select some code to refactor.",
                                "A3sist Refactoring",
                                OLEMSGICON.OLEMSGICON_INFO,
                                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error running refactoring: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static void Initialize(A3sistPackage package, OleMenuCommandService commandService)
        {
            Instance = new RefactorCodeCommand(package, commandService);
        }
    }

    /// <summary>
    /// Command handler for showing the A3sist tool window
    /// </summary>
    internal sealed class ShowA3sistToolWindowCommand
    {
        public const int CommandId = 0x0104;
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789123");
        private readonly AsyncPackage package;

        private ShowA3sistToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ShowA3sistToolWindowCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ShowA3sistToolWindowCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Get the instance of the tool window created when package was initialized
                var window = await this.package.FindToolWindowAsync(typeof(A3sist.UI.A3sistToolWindowPane), 0, true, this.package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                var windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error opening A3sist tool window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static void Initialize(A3sistPackage package, OleMenuCommandService commandService)
        {
            Instance = new ShowA3sistToolWindowCommand(package, commandService);
        }
    }

    /// <summary>
    /// Command handler for showing the A3sist tool window from View menu
    /// </summary>
    internal sealed class ShowA3sistToolWindowViewCommand
    {
        public const int CommandId = 0x0105;
        public static readonly Guid CommandSet = new Guid("12345678-1234-1234-1234-123456789123");
        private readonly AsyncPackage package;

        private ShowA3sistToolWindowViewCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ShowA3sistToolWindowViewCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ShowA3sistToolWindowViewCommand(package, commandService);
        }

        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Get the instance of the tool window created when package was initialized
                var window = await this.package.FindToolWindowAsync(typeof(A3sist.UI.A3sistToolWindowPane), 0, true, this.package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                var windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Error opening A3sist tool window: {ex.Message}",
                    "A3sist Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public static void Initialize(A3sistPackage package, OleMenuCommandService commandService)
        {
            Instance = new ShowA3sistToolWindowViewCommand(package, commandService);
        }
    }
}