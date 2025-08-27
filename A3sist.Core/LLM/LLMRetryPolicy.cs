using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace A3sist.Core.LLM
{
    public static class LLMRetryPolicy
    {
        public static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy(int maxRetries = 3, ILogger? logger = null)
        {
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    maxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        var message = $"Retry {retryCount} due to: {result.Exception?.Message ?? result.Result.StatusCode.ToString()}";
                        logger?.LogWarning(message);
                        Console.WriteLine(message);
                    });
        }
    }
}