using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace A3sist.Core
{
    /// <summary>
    /// Stub Startup class to allow UI project to compile
    /// This is a temporary stub for task 10.1 implementation
    /// </summary>
    public static class Startup
    {
        /// <summary>
        /// Create a service provider
        /// </summary>
        public static IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            
            // Add basic logging
            services.AddLogging(builder => builder.AddDebug());
            
            // Add stub orchestrator
            services.AddSingleton<Services.IOrchestrator, StubOrchestrator>();
            
            return services.BuildServiceProvider();
        }
    }
    
    /// <summary>
    /// Stub orchestrator implementation
    /// </summary>
    public class StubOrchestrator : Services.IOrchestrator
    {
        public async Task<A3sist.Shared.Messaging.AgentResult> ProcessRequestAsync(
            A3sist.Shared.Messaging.AgentRequest request, 
            System.Threading.CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken); // Simulate processing
            
            return new A3sist.Shared.Messaging.AgentResult
            {
                Success = true,
                Message = "Stub implementation - request processed",
                Content = "This is a placeholder response from the stub orchestrator",
                AgentName = "StubOrchestrator",
                ProcessingTime = TimeSpan.FromMilliseconds(100)
            };
        }
    }
}