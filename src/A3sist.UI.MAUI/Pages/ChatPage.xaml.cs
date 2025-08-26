using System.Collections.ObjectModel;

namespace A3sist.UI.MAUI;

public partial class ChatPage : ContentPage
{
    private readonly ObservableCollection<string> _messages;

    public ChatPage()
    {
        InitializeComponent();
        _messages = new ObservableCollection<string>();
        ChatCollectionView.ItemsSource = _messages;
        
        // Add a welcome message
        _messages.Add("A3sist: Hello! How can I help you with your code today?");
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(MessageEntry.Text))
        {
            // Add user message
            _messages.Add($"You: {MessageEntry.Text}");
            
            var userMessage = MessageEntry.Text;
            MessageEntry.Text = string.Empty;
            
            // Simulate AI response (in real implementation, this would call the AI service)
            await Task.Delay(1000);
            _messages.Add($"A3sist: I received your message: '{userMessage}'. This is a placeholder response.");
        }
    }
}