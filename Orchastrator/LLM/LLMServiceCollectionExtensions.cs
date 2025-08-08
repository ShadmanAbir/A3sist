using Microsoft.Extensions.DependencyInjection;

public static class LLMServiceCollectionExtensions
{
    public static IServiceCollection AddLLMClient(this IServiceCollection services, string baseUrl = "http://localhost:11434")
    {
        services.AddHttpClient&lt;ILLMClient, CodestralLLMClient&gt;(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}