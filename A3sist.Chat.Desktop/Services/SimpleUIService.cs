using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace A3sist.Chat.Desktop
{
    /// <summary>
    /// Simple UI service implementation for the standalone desktop application
    /// </summary>
    public class SimpleUIService : IUIService
    {
        private readonly ILogger<SimpleUIService> _logger;

        public SimpleUIService(ILogger<SimpleUIService> logger)
        {
            _logger = logger;
        }

        public async Task ShowNotificationAsync(string title, string message)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("Showing notification: {Title} - {Message}", title, message);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                });
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return await Task.Run(() =>
            {
                _logger.LogInformation("Showing confirmation: {Title} - {Message}", title, message);
                
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    return result == MessageBoxResult.Yes;
                });
            });
        }

        public async Task<string> GetSelectedCodeAsync()
        {
            await Task.CompletedTask;
            
            // In a real implementation, this would get selected code from an editor
            // For now, return sample code
            return @"// Sample selected code
public class Example
{
    public void SampleMethod()
    {
        Console.WriteLine(""This is sample selected code"");
    }
}";
        }
    }
}