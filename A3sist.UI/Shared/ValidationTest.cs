using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.UI.Shared.Interfaces;
using A3sist.Core.Services;

namespace A3sist.UI.Shared
{
    /// <summary>
    /// Simple validation test for the unified architecture
    /// </summary>
    public class ValidationTest
    {
        /// <summary>
        /// Tests the basic service registration and RAG functionality
        /// </summary>
        public static async Task<bool> TestUnifiedArchitectureAsync()
        {
            try
            {
                // Create service collection and register services
                var services = new ServiceCollection();
                
                // Add basic logging
                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                    builder.AddConsole();
                });

                // Add our unified UI services
                services.AddA3sistUI();
                services.AddRAGServices();
                services.AddFrameworkLogging();

                // Build service provider
                var serviceProvider = services.BuildServiceProvider();

                // Test service resolution
                var uiService = serviceProvider.GetService<IUIService>();
                var chatService = serviceProvider.GetService<IChatService>();
                var ragUIService = serviceProvider.GetService<IRAGUIService>();
                var router = serviceProvider.GetService<EnhancedRequestRouter>();

                // Validate services are not null
                if (uiService == null)
                {
                    Console.WriteLine("❌ IUIService not registered properly");
                    return false;
                }

                if (chatService == null)
                {
                    Console.WriteLine("❌ IChatService not registered properly");
                    return false;
                }

                if (ragUIService == null)
                {
                    Console.WriteLine("❌ IRAGUIService not registered properly");
                    return false;
                }

                if (router == null)
                {
                    Console.WriteLine("❌ EnhancedRequestRouter not registered properly");
                    return false;
                }

                Console.WriteLine("✅ All core services registered successfully");

                // Test framework-specific implementations
#if NET472
                Console.WriteLine("🏢 Running on .NET Framework 4.7.2 (VSIX)");
                if (uiService.GetType().Name != "VSIXUIService")
                {
                    Console.WriteLine($"❌ Expected VSIXUIService, got {uiService.GetType().Name}");
                    return false;
                }
#endif

#if NET9_0_OR_GREATER
                Console.WriteLine("🖥️ Running on .NET 9 (WPF)");
                if (uiService.GetType().Name != "WPFUIService")
                {
                    Console.WriteLine($"❌ Expected WPFUIService, got {uiService.GetType().Name}");
                    return false;
                }
#endif

                Console.WriteLine("✅ Framework-specific services loaded correctly");

                // Test basic functionality
                var chatViewModel = serviceProvider.GetService<ChatViewModel>();
                if (chatViewModel == null)
                {
                    Console.WriteLine("❌ ChatViewModel not available");
                    return false;
                }

                Console.WriteLine("✅ View models created successfully");

                // Test RAG service if available
                var ragService = serviceProvider.GetService<RAGService>();
                if (ragService != null)
                {
                    Console.WriteLine("✅ RAG service available");
                }
                else
                {
                    Console.WriteLine("⚠️ RAG service not available (missing dependencies)");
                }

                Console.WriteLine("🎉 Unified architecture validation completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Validation failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Tests the compilation conditional directives
        /// </summary>
        public static void TestConditionalCompilation()
        {
            Console.WriteLine("🧪 Testing conditional compilation:");

#if NET472
            Console.WriteLine("  ✅ NET472 directive active");
#else
            Console.WriteLine("  ❌ NET472 directive not active");
#endif

#if NET9_0_OR_GREATER
            Console.WriteLine("  ✅ NET9_0_OR_GREATER directive active");
#else
            Console.WriteLine("  ❌ NET9_0_OR_GREATER directive not active");
#endif

#if DEBUG
            Console.WriteLine("  🐛 DEBUG mode active");
#else
            Console.WriteLine("  🚀 RELEASE mode active");
#endif
        }

        /// <summary>
        /// Simple performance test for the simplified architecture
        /// </summary>
        public static async Task<TimeSpan> TestPerformanceAsync()
        {
            var startTime = DateTime.UtcNow;
            
            // Simulate multiple service creations (should be fast with simplified architecture)
            for (int i = 0; i < 100; i++)
            {
                var services = new ServiceCollection();
                services.AddA3sistUI();
                services.AddFrameworkLogging();
                
                var serviceProvider = services.BuildServiceProvider();
                var uiService = serviceProvider.GetService<IUIService>();
                
                // Cleanup
                serviceProvider.Dispose();
            }
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            Console.WriteLine($"⚡ Performance test: Created 100 service containers in {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"   Average: {duration.TotalMilliseconds / 100:F2}ms per container");
            
            return duration;
        }
    }
}