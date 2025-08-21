using Microsoft.Extensions.DependencyInjection;
using System;

namespace A3sist.Orchastrator.LLM
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