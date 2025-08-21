using System.Diagnostics;

namespace A3sist.TestUtilities;

/// <summary>
/// Helper methods for testing async operations
/// </summary>
public static class AsyncTestHelpers
{
    /// <summary>
    /// Executes an async operation and measures its execution time
    /// </summary>
    public static async Task<(T Result, TimeSpan Duration)> MeasureAsync<T>(Func<Task<T>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await operation();
        stopwatch.Stop();
        return (result, stopwatch.Elapsed);
    }

    /// <summary>
    /// Executes an async operation and measures its execution time (void return)
    /// </summary>
    public static async Task<TimeSpan> MeasureAsync(Func<Task> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        await operation();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Waits for a condition to be true with timeout
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition, 
        TimeSpan timeout, 
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (condition())
                return true;

            await Task.Delay(interval);
        }

        return false;
    }

    /// <summary>
    /// Waits for an async condition to be true with timeout
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> condition, 
        TimeSpan timeout, 
        TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (await condition())
                return true;

            await Task.Delay(interval);
        }

        return false;
    }

    /// <summary>
    /// Executes multiple async operations concurrently and returns their results
    /// </summary>
    public static async Task<T[]> ExecuteConcurrentlyAsync<T>(params Func<Task<T>>[] operations)
    {
        var tasks = operations.Select(op => op()).ToArray();
        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an operation multiple times concurrently for load testing
    /// </summary>
    public static async Task<T[]> ExecuteLoadTestAsync<T>(Func<Task<T>> operation, int concurrentCount)
    {
        var tasks = Enumerable.Range(0, concurrentCount)
            .Select(_ => operation())
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an operation with a timeout
    /// </summary>
    public static async Task<T> WithTimeoutAsync<T>(Task<T> task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        
        if (completedTask == task)
        {
            cts.Cancel(); // Cancel the delay task
            return await task;
        }
        
        throw new TimeoutException($"Operation timed out after {timeout}");
    }

    /// <summary>
    /// Executes an operation with a timeout (void return)
    /// </summary>
    public static async Task WithTimeoutAsync(Task task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        
        if (completedTask == task)
        {
            cts.Cancel(); // Cancel the delay task
            await task;
            return;
        }
        
        throw new TimeoutException($"Operation timed out after {timeout}");
    }

    /// <summary>
    /// Retries an async operation with exponential backoff
    /// </summary>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> operation, 
        int maxAttempts = 3, 
        TimeSpan? initialDelay = null)
    {
        var delay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt == maxAttempts)
                    break;

                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {maxAttempts} attempts", 
            lastException);
    }
}