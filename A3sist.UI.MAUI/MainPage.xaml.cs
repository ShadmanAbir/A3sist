using Microsoft.Extensions.Logging;

namespace A3sist.UI.MAUI;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage>? _logger;
    private int _clickCount = 0;

    public MainPage()
    {
        InitializeComponent();
        
        // Get logger from DI container if available
        try
        {
            _logger = Handler?.MauiContext?.Services?.GetService<ILogger<MainPage>>();
        }
        catch
        {
            // Logger not available, continue without it
        }
        
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        try
        {
            StatusLabel.Text = "✅ System Ready";
            AgentCountLabel.Text = "🤖 Agents: 3 Active";
            
            _logger?.LogInformation("MainPage status updated");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating status");
            StatusLabel.Text = "❌ Error loading status";
        }
    }

    private async void OnChatClicked(object sender, EventArgs e)
    {
        try
        {
            _clickCount++;
            ChatBtn.Text = $"💬 Open Chat ({_clickCount})";
            
            _logger?.LogInformation("Chat button clicked");
            
            // Navigate to chat page
            await Shell.Current.GoToAsync("//chat");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error navigating to chat");
            await DisplayAlert("Error", "Failed to open chat", "OK");
        }
    }

    private async void OnAgentsClicked(object sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("Agents button clicked");
            
            // Show agent status
            await DisplayAlert("Agents", 
                "Agent Status:\n\n" +
                "• C# Agent: Active ✅\n" +
                "• Chat Agent: Active ✅\n" +
                "• Fixer Agent: Standby 🟡\n" +
                "• Designer Agent: Active ✅", 
                "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing agents");
            await DisplayAlert("Error", "Failed to load agent status", "OK");
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            _logger?.LogInformation("Settings button clicked");
            
            // Show settings
            await DisplayAlert("Settings", 
                "A3sist Settings:\n\n" +
                "• Version: 1.0.0\n" +
                "• .NET: 9.0\n" +
                "• MAUI Framework: Active\n" +
                "• VSIX Host: Connected", 
                "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing settings");
            await DisplayAlert("Error", "Failed to load settings", "OK");
        }
    }
}