using A3sist.Core.LLM;
using A3sist.Shared.Interfaces;
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


                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();


                var llmClient = serviceProvider.GetRequiredService<ILLMClient>();

                // Example LLM usage
                var response = await llmClient.CompleteAsync("Analyze this code: public class Broken { public void Method() { } }");

                // Output the response
                Console.WriteLine(response);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}