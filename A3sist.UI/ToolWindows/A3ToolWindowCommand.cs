using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace A3sist.UI
{
    /// <summary>
    /// A command for showing a tool window.
    /// </summary>
    [VisualStudioContribution]
    public class A3ToolWindowCommand : Command
    {
        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new(displayName: "732e41d0-846d-418a-acef-f135b9f75e41")
        {
            // Use this object initializer to set optional parameters for the command. The required parameter,
            // displayName, is set above. To localize the displayName, add an entry in .vsextension\string-resources.json
            // and reference it here by passing "%UI.A3ToolWindowCommand.DisplayName%" as a constructor parameter.
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            await Extensibility.Shell().ShowToolWindowAsync<A3ToolWindow>(activate: true, cancellationToken);
        }
    }
}
