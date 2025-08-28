using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace A3sist.UI
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided by the Managed Package Framework
    /// which provides an implementation of a tool window that is compatible with the Visual Studio shell.
    /// </summary>
    [Guid("285cd009-b086-4f05-a305-35790de8f3d1")]
    public class A3sistToolWindowPane : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="A3sistToolWindowPane"/> class.
        /// </summary>
        public A3sistToolWindowPane() : base(null)
        {
            this.Caption = "A3sist";
            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new A3sistToolWindow();
        }
    }
}