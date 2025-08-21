using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace A3sist.UI.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework
    /// and implements the IVsWindowPane interface that is required by the shell to host the window.
    /// </para>
    /// </remarks>
    [Guid("11111111-2222-3333-4444-555555555555")]
    public class AgentStatusWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentStatusWindow"/> class.
        /// </summary>
        public AgentStatusWindow() : base(null)
        {
            this.Caption = "A3sist Agent Status";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new AgentStatusWindowControl();
        }
    }
}