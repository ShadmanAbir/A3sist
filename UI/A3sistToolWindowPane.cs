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
    [Guid("a3bce2e1-8f4c-4b2d-9a7f-5c1e3d2b4a6f")]
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