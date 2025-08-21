using A3sist.Orchastrator.LLM;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Orchastrator.LLM
{
    public static class LLMServiceCollectionExtensions
    {
        public static IServiceCollection AddLLM(this IServiceCollection services)
        {
            try
            {
                services.AddSingleton<ILLMClient, CodestralLLMClient>();
                services.AddSingleton<LLMCacheService>();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while adding LLM services: {ex.Message}");
                throw;
            }

            return services;
        }
    }
}