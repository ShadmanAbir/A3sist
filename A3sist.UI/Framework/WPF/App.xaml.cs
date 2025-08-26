#if NET9_0_OR_GREATER

using System;
using System.Threading.Tasks;
using System.Windows;
using A3sist.UI.Shared;

namespace A3sist.UI.Framework.WPF
{
    /// <summary>
    /// Main entry point for WPF application (.NET 9)
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider? Services { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Run validation test
                Console.WriteLine("🚀 Starting A3sist WPF Application (.NET 9)");
                
                ValidationTest.TestConditionalCompilation();
                
                var testResult = await ValidationTest.TestUnifiedArchitectureAsync();
                if (!testResult)
                {
                    Console.WriteLine("❌ Validation failed, exiting...");
                    Shutdown(1);
                    return;
                }

                var performanceResult = await ValidationTest.TestPerformanceAsync();
                Console.WriteLine($"✅ Performance benchmark completed: {performanceResult.TotalMilliseconds:F2}ms");

                // Configure services
                var services = new ServiceCollection();
                services.AddA3sistUI();
                services.AddRAGServices();
                services.AddFrameworkLogging();
                Services = services.BuildServiceProvider();

                // Create and show main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                MainWindow = mainWindow;

                Console.WriteLine("✅ A3sist WPF Application started successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start application: {ex.Message}");
                MessageBox.Show($"Failed to start A3sist: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Console.WriteLine("🛑 Shutting down A3sist WPF Application");
                
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

    /// <summary>
    /// Main window for WPF application
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "A3sist - AI-Powered Coding Assistant";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Set up the main content
            var chatView = new Views.ChatView();
            Content = chatView;
        }

        public void UpdateKnowledgeStatus(string status)
        {
            // Update UI with knowledge status
            Title = $"A3sist - {status}";
        }
    }
}

#endif