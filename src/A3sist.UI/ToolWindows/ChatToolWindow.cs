using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using A3sist.UI.Components.Chat;

namespace A3sist.UI.ToolWindows
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts the user control.
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework (MPF)
    /// and uses the user control defined in ChatToolWindowControl.xaml
    /// </summary>
    [Guid("4E8B5F7D-8C9A-4B2D-9E1F-3A5C7B8D4E6F")]
    public class ChatToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatToolWindow"/> class.
        /// </summary>
        public ChatToolWindow() : base(null)
        {
            Caption = "A3sist Chat";
            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new ChatToolWindowControl();
        }

        /// <summary>
        /// Gets the chat control hosted in this tool window
        /// </summary>
        public ChatToolWindowControl ChatControl => Content as ChatToolWindowControl;
    }
}