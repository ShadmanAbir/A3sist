using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using A3sist.UI.Services;

namespace A3sist.UI
{
    /// <summary>
    /// Main A3sist agent interaction tool window with comprehensive UI features
    /// </summary>
    [VisualStudioContribution]
    [Guid("732e41d0-846d-418a-acef-f135b9f75e41")]
    public class A3ToolWindow : ToolWindow
    {
        private readonly A3ToolWindowContent content = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="A3ToolWindow" /> class.
        /// </summary>
        public A3ToolWindow()
        {
            Title = "A3sist - AI Assistant";
        }

        /// <inheritdoc />
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
            // Configure tool window placement and behavior
            Placement = ToolWindowPlacement.Floating,
            AllowAutoCreation = true,
            DockDirection = ToolWindowDockDirection.Tabbed
        };

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Initialize notification service
            var notificationService = ProgressNotificationService.Instance;
            
            // Show welcome notification
            notificationService.ShowInfo("A3sist Ready", "AI Assistant is ready to help with your code");

            // Perform any additional initialization
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task<IRemoteUserControl> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IRemoteUserControl>(content);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content?.Dispose();
                
                // Clean up any resources
                var notificationService = ProgressNotificationService.Instance;
                notificationService.ShowInfo("A3sist Closed", "AI Assistant tool window closed");
            }

            base.Dispose(disposing);
        }
    }
}
