using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using A3sist.Shared.Enums;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// Converts boolean values to Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }

    /// <summary>
    /// Converts connection status to color
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public static readonly StatusToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionStatus status)
            {
                return status switch
                {
                    ConnectionStatus.Connected => Brushes.Green,
                    ConnectionStatus.Connecting => Brushes.Yellow,
                    ConnectionStatus.Disconnected => Brushes.Red,
                    ConnectionStatus.Error => Brushes.OrangeRed,
                    _ => Brushes.Gray
                };
            }
            
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Template selector for chat messages
    /// </summary>
    public class ChatMessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserTemplate { get; set; }
        public DataTemplate AssistantTemplate { get; set; }
        public DataTemplate SystemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ChatMessage message)
            {
                return message.Type switch
                {
                    ChatMessageType.User => UserTemplate,
                    ChatMessageType.Assistant => AssistantTemplate,
                    ChatMessageType.System => SystemTemplate,
                    _ => UserTemplate
                };
            }

            return UserTemplate;
        }
    }

    /// <summary>
    /// Custom control for rendering markdown text
    /// </summary>
    public class MarkdownTextBlock : Control
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(MarkdownTextBlock),
            new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownTextBlock control)
            {
                control.UpdateContent();
            }
        }

        private void UpdateContent()
        {
            // Simple markdown parsing for basic formatting
            // In a full implementation, you'd use a proper markdown parser
            var content = ProcessMarkdown(Text);
            
            // Set content to a TextBlock (simplified)
            if (Template?.FindName("PART_ContentPresenter", this) is ContentPresenter presenter)
            {
                presenter.Content = new TextBlock 
                { 
                    Text = content,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Foreground,
                    FontSize = FontSize
                };
            }
        }

        private string ProcessMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Simple markdown processing (replace with full parser in production)
            var processed = text
                .Replace("**", "") // Bold (simplified)
                .Replace("*", "")  // Italic (simplified)
                .Replace("`", ""); // Code (simplified)

            return processed;
        }

        static MarkdownTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownTextBlock), 
                new FrameworkPropertyMetadata(typeof(MarkdownTextBlock)));
        }
    }

    /// <summary>
    /// Animated typing dots indicator
    /// </summary>
    public class TypingDotsAnimation : Control
    {
        private readonly DispatcherTimer _timer;
        private int _currentDot = 0;

        public TypingDotsAnimation()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
            
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _currentDot = (_currentDot + 1) % 4;
            
            if (Template?.FindName("PART_DotsText", this) is TextBlock dotsText)
            {
                dotsText.Text = new string('.', _currentDot);
            }
        }

        static TypingDotsAnimation()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TypingDotsAnimation),
                new FrameworkPropertyMetadata(typeof(TypingDotsAnimation)));
        }
    }
}

namespace A3sist.Shared.Enums
{
    /// <summary>
    /// Connection status for the chat interface
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Type of chat message
    /// </summary>
    public enum ChatMessageType
    {
        User,
        Assistant,
        System
    }
}