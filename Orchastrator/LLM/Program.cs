using A3sist.Orchastrator.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;


namespace A3sist.Orchastrator.LLM
{
    /// <summary>
    /// The entry point for the LLM service.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static async Task Main(string[] args)
        {
            try
            {
                var services = new ServiceCollection();

                // Configure logging
                //services.AddLogging(configure => configure.AddConsole());

                // Register services
                services.AddHttpClient<ILLMClient, CodestralLLMClient>();
                //services.AddTransient<FixerAgent>();

                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();

                // Get the FixerAgent service
                var fixerAgent = serviceProvider.GetRequiredService<FileEditorAgent>();

                // Fix code example
                var fixedCode = await fixerAgent.FixCodeAsync("public class Broken { public void Method() { } }");

                // Output the fixed code
                Console.WriteLine(fixedCode);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}