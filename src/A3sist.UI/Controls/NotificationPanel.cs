using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using A3sist.UI.Services;

namespace A3sist.UI.Controls
{
    /// <summary>
    /// Control for displaying notifications and progress indicators
    /// </summary>
    public class NotificationPanel : UserControl
    {
        private readonly ProgressNotificationService _notificationService;

        public NotificationPanel()
        {
            _notificationService = ProgressNotificationService.Instance;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Create main container
            var mainPanel = new DockPanel();

            // Create progress section
            var progressSection = CreateProgressSection();
            DockPanel.SetDock(progressSection, Dock.Top);
            mainPanel.Children.Add(progressSection);

            // Create notifications section
            var notificationsSection = CreateNotificationsSection();
            mainPanel.Children.Add(notificationsSection);

            this.Content = mainPanel;
        }

        private Border CreateProgressSection()
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 10, 5)
            };

            var panel = new StackPanel();

            // Progress bar
            var progressBar = new ProgressBar
            {
                Height = 4,
                Margin = new Thickness(0, 0, 0, 5)
            };
            progressBar.SetBinding(ProgressBar.ValueProperty, new Binding("OverallProgress") { Source = _notificationService });
            progressBar.SetBinding(ProgressBar.VisibilityProperty, new Binding("HasActiveOperations") 
            { 
                Source = _notificationService,
                Converter = new BooleanToVisibilityConverter()
            });

            // Status text
            var statusText = new TextBlock
            {
                FontSize = 12,
                Foreground = Brushes.Gray
            };
            statusText.SetBinding(TextBlock.TextProperty, new Binding("CurrentOperationText") { Source = _notificationService });

            panel.Children.Add(progressBar);
            panel.Children.Add(statusText);
            border.Child = panel;

            return border;
        }

        private GroupBox CreateNotificationsSection()
        {
            var groupBox = new GroupBox
            {
                Header = "Notifications",
                MaxHeight = 300
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var listBox = new ListBox
            {
                BorderThickness = new Thickness(0)
            };
            listBox.SetBinding(ListBox.ItemsSourceProperty, new Binding("Notifications") { Source = _notificationService });

            // Create data template for notification items
            var dataTemplate = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.PaddingProperty, new Thickness(8));
            factory.SetValue(Border.MarginProperty, new Thickness(2));
            factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            factory.SetBinding(Border.BackgroundProperty, new Binding("Type") { Converter = new NotificationTypeToColorConverter() });

            var innerPanel = new FrameworkElementFactory(typeof(StackPanel));

            // Title
            var titleText = new FrameworkElementFactory(typeof(TextBlock));
            titleText.SetBinding(TextBlock.TextProperty, new Binding("Title"));
            titleText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
            titleText.SetValue(TextBlock.FontSizeProperty, 12.0);
            innerPanel.AppendChild(titleText);

            // Message
            var messageText = new FrameworkElementFactory(typeof(TextBlock));
            messageText.SetBinding(TextBlock.TextProperty, new Binding("Message"));
            messageText.SetValue(TextBlock.FontSizeProperty, 11.0);
            messageText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            messageText.SetValue(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0));
            innerPanel.AppendChild(messageText);

            // Timestamp
            var timestampText = new FrameworkElementFactory(typeof(TextBlock));
            timestampText.SetBinding(TextBlock.TextProperty, new Binding("TimeAgo"));
            timestampText.SetValue(TextBlock.FontSizeProperty, 10.0);
            timestampText.SetValue(TextBlock.ForegroundProperty, Brushes.Gray);
            timestampText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            timestampText.SetValue(TextBlock.MarginProperty, new Thickness(0, 2, 0, 0));
            innerPanel.AppendChild(timestampText);

            factory.AppendChild(innerPanel);
            dataTemplate.VisualTree = factory;
            listBox.ItemTemplate = dataTemplate;

            scrollViewer.Content = listBox;
            groupBox.Content = scrollViewer;

            return groupBox;
        }
    }

    /// <summary>
    /// Converter for notification type to background color
    /// </summary>
    public class NotificationTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.Success => new SolidColorBrush(Color.FromRgb(212, 237, 218)),
                    NotificationType.Warning => new SolidColorBrush(Color.FromRgb(255, 243, 205)),
                    NotificationType.Error => new SolidColorBrush(Color.FromRgb(248, 215, 218)),
                    NotificationType.Info => new SolidColorBrush(Color.FromRgb(209, 236, 241)),
                    _ => Brushes.Transparent
                };
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.Windows.DependencyProperty.UnsetValue;
        }
    }
}