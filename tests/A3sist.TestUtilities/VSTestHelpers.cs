using Moq;

namespace A3sist.TestUtilities;

/// <summary>
/// Helper methods for Visual Studio integration testing
/// </summary>
public static class VSTestHelpers
{
    /// <summary>
    /// Creates a mock IServiceProvider for testing
    /// </summary>
    public static Mock<IServiceProvider> CreateMockServiceProvider()
    {
        var mock = new Mock<IServiceProvider>();
        return mock;
    }

    /// <summary>
    /// Simulates a VS command execution context
    /// </summary>
    public static void SimulateCommandExecution(Action commandHandler)
    {
        try
        {
            // Simulate the command execution environment
            commandHandler?.Invoke();
        }
        catch (Exception ex)
        {
            // Log or handle command execution errors
            throw new InvalidOperationException("Command execution failed", ex);
        }
    }

    /// <summary>
    /// Creates a test environment for testing
    /// </summary>
    public static TestEnvironment CreateTestEnvironment()
    {
        return new TestEnvironment();
    }
}

/// <summary>
/// Test environment for testing
/// </summary>
public class TestEnvironment : IDisposable
{
    public Mock<IServiceProvider> ServiceProvider { get; private set; }

    public TestEnvironment()
    {
        ServiceProvider = VSTestHelpers.CreateMockServiceProvider();
    }

    public void Dispose()
    {
        // Cleanup test environment
        GC.SuppressFinalize(this);
    }
}