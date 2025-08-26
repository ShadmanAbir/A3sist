using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.Chat.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Console.WriteLine("🚀 Starting A3sist Chat Desktop Application");

                // Configure services
                var services = new ServiceCollection();
                ConfigureServices(services);
                Services = services.BuildServiceProvider();

                // Create and show main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                MainWindow = mainWindow;

                Console.WriteLine("✅ A3sist Chat Desktop Application started successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start application: {ex.Message}");
                MessageBox.Show($"Failed to start A3sist Chat Desktop: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add simple chat services
            services.AddSingleton<IChatService, SimpleChatService>();
            services.AddSingleton<IUIService, SimpleUIService>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Console.WriteLine("🛑 Shutting down A3sist Chat Desktop Application");
                
                if (Services is IDisposable disposableServices)
                {
                    disposableServices.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error during shutdown: {ex.Message}");
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}