using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class LLMRetryPolicy
{
    public static AsyncRetryPolicy&lt;HttpResponseMessage&gt; CreateRetryPolicy(int maxRetries = 3)
    {
        return Policy&lt;HttpResponseMessage&gt;
            .Handle&lt;HttpRequestException&gt;()
            .OrResult(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (result, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} due to: {result.Exception?.Message ?? result.Result.StatusCode.ToString()}");
                });
    }
}