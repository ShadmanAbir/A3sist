using System.Windows;
using System.Windows.Controls;

namespace A3sist.UI.ToolWindows
{
    /// <summary>
    /// Interaction logic for AgentStatusWindowControl.
    /// </summary>
    public partial class AgentStatusWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentStatusWindowControl"/> class.
        /// </summary>
        public AgentStatusWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initialize the component
        /// </summary>
        private void InitializeComponent()
        {
            // Create the main grid
            var mainGrid = new Grid();
            
            // Add title
            var titleTextBlock = new TextBlock
            {
                Text = "A3sist Agent Status Monitor",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Add placeholder content
            var contentTextBlock = new TextBlock
            {
                Text = "Agent status monitoring will be implemented in task 10.2",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Add elements to grid
            mainGrid.Children.Add(titleTextBlock);
            mainGrid.Children.Add(contentTextBlock);

            // Set as content
            this.Content = mainGrid;
        }
    }
}