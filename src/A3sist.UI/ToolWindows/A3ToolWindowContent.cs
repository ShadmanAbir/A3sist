using Microsoft.VisualStudio.Extensibility.UI;

namespace A3sist.UI
{
    /// <summary>
    /// A remote user control to use as tool window UI content.
    /// </summary>
    internal class A3ToolWindowContent : RemoteUserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="A3ToolWindowContent" /> class.
        /// </summary>
        public A3ToolWindowContent()
            : base(dataContext: new A3ToolWindowData())
        {
        }
    }
}
