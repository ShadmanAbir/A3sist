using Microsoft.Extensions.Configuration;

namespace A3sist.TestUtilities;

/// <summary>
/// Test configuration provider with default values for testing
/// </summary>
public class TestConfiguration
{
    private readonly Dictionary<string, string> _values;

    public TestConfiguration()
    {
        _values = new Dictionary<string, string>
        {
            ["A3sist:Agents:Orchestrator:Enabled"] = "true",
            ["A3sist:Agents:Orchestrator:MaxConcurrentTasks"] = "5",
            ["A3sist:Agents:Orchestrator:Timeout"] = "00:05:00",
            ["A3sist:Agents:CSharpAgent:Enabled"] = "true",
            ["A3sist:Agents:CSharpAgent:AnalysisLevel"] = "Full",
            ["A3sist:LLM:Provider"] = "Test",
            ["A3sist:LLM:Model"] = "test-model",
            ["A3sist:LLM:MaxTokens"] = "4000",
            ["A3sist:Logging:Level"] = "Information",
            ["A3sist:Logging:OutputPath"] = Path.GetTempPath()
        };
    }

    public IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(_values!)
            .Build();
    }

    public void SetValue(string key, string value)
    {
        _values[key] = value;
    }

    public string? GetValue(string key)
    {
        return _values.TryGetValue(key, out var value) ? value : null;
    }
}