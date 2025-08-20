using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace A3sist.Services
{
    public class RoutingMiddleware
    {
        private readonly ContextRouter _router;
        private readonly Stopwatch _performanceTimer = new Stopwatch();

        public RoutingMiddleware(ContextRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public async Task<object> ProcessRequestAsync(string contextType, string serializedContext)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (string.IsNullOrEmpty(serializedContext))
                throw new ArgumentNullException(nameof(serializedContext));

            _performanceTimer.Restart();

            try
            {
                // Log the request
                Console.WriteLine($"Processing request for context type: {contextType}");

                // Route the context
                await _router.RouteContextAsync(contextType, serializedContext);

                // Log successful processing
                Console.WriteLine($"Successfully processed context type: {contextType}");

                // Return a success response
                return new
                {
                    Status = "Success",
                    ContextType = contextType,
                    ProcessingTime = _performanceTimer.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error processing context type {contextType}: {ex.Message}");

                // Return an error response
                return new
                {
                    Status = "Error",
                    ContextType = contextType,
                    ErrorMessage = ex.Message,
                    ProcessingTime = _performanceTimer.ElapsedMilliseconds
                };
            }
            finally
            {
                _performanceTimer.Stop();
            }
        }

        public async Task<object> ProcessRequestWithRetryAsync(string contextType, string serializedContext, int maxRetries = 3)
        {
            if (string.IsNullOrEmpty(contextType))
                throw new ArgumentNullException(nameof(contextType));

            if (string.IsNullOrEmpty(serializedContext))
                throw new ArgumentNullException(nameof(serializedContext));

            if (maxRetries < 1)
                throw new ArgumentException("Max retries must be at least 1", nameof(maxRetries));

            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    return await ProcessRequestAsync(contextType, serializedContext);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    Console.WriteLine($"Attempt {retryCount} failed. Retrying...");
                    await Task.Delay(1000 * retryCount); // Exponential backoff
                }
            }

            // If all retries failed, return the last error
            return new
            {
                Status = "Error",
                ContextType = contextType,
                ErrorMessage = $"All {maxRetries} attempts failed. Last error: {lastException?.Message}",
                ProcessingTime = _performanceTimer.ElapsedMilliseconds
            };
        }
    }
}