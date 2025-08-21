using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.ToolWindows;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.UI
{
    /// <summary>
    /// A sample tool window.
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
            Title = "A3sist";
        }

        /// <inheritdoc />
        public override ToolWindowConfiguration ToolWindowConfiguration => new()
        {
            // Use this object initializer to set optional parameters for the tool window.
            Placement = ToolWindowPlacement.Floating,
        };

        /// <inheritdoc />
        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Use InitializeAsync for any one-time setup or initialization.
            return Task.CompletedTask;
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
                content.Dispose();

            base.Dispose(disposing);
        }
    }
}
