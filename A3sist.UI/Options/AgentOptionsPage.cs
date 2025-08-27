using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace A3sist.UI.Options;

/// <summary>
/// Agent configuration options page
/// </summary>
[ComVisible(true)]
[Guid("12345678-1234-1234-1234-123456789013")]
public class AgentOptionsPage : BaseOptionsPage
{
    public override string CategoryName => "A3sist";
    public override string PageName => "Agents";

    [Category("Orchestrator")]
    [DisplayName("Enable Orchestrator")]
    [Description("Enable the main orchestrator agent")]
    public bool EnableOrchestrator { get; set; } = true;

    [Category("Orchestrator")]
    [DisplayName("Max concurrent tasks")]
    [Description("Maximum number of concurrent tasks for the orchestrator")]
    public int OrchestratorMaxConcurrentTasks { get; set; } = 5;

    [Category("Orchestrator")]
    [DisplayName("Timeout (seconds)")]
    [Description("Timeout for orchestrator operations in seconds")]
    public int OrchestratorTimeoutSeconds { get; set; } = 300;

    [Category("Language Agents")]
    [DisplayName("Enable C# Agent")]
    [Description("Enable the C# language agent")]
    public bool EnableCSharpAgent { get; set; } = true;

    [Category("Language Agents")]
    [DisplayName("C# Analysis Level")]
    [Description("Analysis level for C# code (Basic, Full, Deep)")]
    public string CSharpAnalysisLevel { get; set; } = "Full";

    [Category("Language Agents")]
    [DisplayName("Enable JavaScript Agent")]
    [Description("Enable the JavaScript/TypeScript language agent")]
    public bool EnableJavaScriptAgent { get; set; } = true;

    [Category("Language Agents")]
    [DisplayName("Enable Python Agent")]
    [Description("Enable the Python language agent")]
    public bool EnablePythonAgent { get; set; } = true;

    [Category("Task Agents")]
    [DisplayName("Enable Fixer Agent")]
    [Description("Enable the code fixing agent")]


    [Category("Task Agents")]
    [DisplayName("Enable Refactor Agent")]
    [Description("Enable the code refactoring agent")]
    public bool EnableRefactorAgent { get; set; } = true;

    [Category("Task Agents")]
    [DisplayName("Enable Validator Agent")]
    [Description("Enable the code validation agent")]
    public bool EnableValidatorAgent { get; set; } = true;

    [Category("Utility Agents")]
    [DisplayName("Enable Knowledge Agent")]
    [Description("Enable the knowledge base agent")]
    public bool EnableKnowledgeAgent { get; set; } = true;

    [Category("Utility Agents")]
    [DisplayName("Enable Shell Agent")]
    [Description("Enable the shell command execution agent")]
    public bool EnableShellAgent { get; set; } = false;

    [Category("Utility Agents")]
    [DisplayName("Enable Training Data Generator")]
    [Description("Enable the training data generation agent")]
    public bool EnableTrainingDataGenerator { get; set; } = false;

    [Category("Agent Behavior")]
    [DisplayName("Auto-retry failed tasks")]
    [Description("Automatically retry failed tasks")]
    public bool AutoRetryFailedTasks { get; set; } = true;

    [Category("Agent Behavior")]
    [DisplayName("Max retry attempts")]
    [Description("Maximum number of retry attempts for failed tasks")]
    public int MaxRetryAttempts { get; set; } = 3;

    [Category("Agent Behavior")]
    [DisplayName("Retry delay (seconds)")]
    [Description("Delay between retry attempts in seconds")]
    public int RetryDelaySeconds { get; set; } = 5;

    [Category("Agent Behavior")]
    [DisplayName("Enable agent health monitoring")]
    [Description("Enable monitoring of agent health and performance")]
    public bool EnableAgentHealthMonitoring { get; set; } = true;

    [Category("Agent Behavior")]
    [DisplayName("Health check interval (seconds)")]
    [Description("Interval for agent health checks in seconds")]
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    public override bool ValidateSettings()
    {
        if (OrchestratorMaxConcurrentTasks < 1 || OrchestratorMaxConcurrentTasks > 100)
        {
            return false;
        }

        if (OrchestratorTimeoutSeconds < 10 || OrchestratorTimeoutSeconds > 3600)
        {
            return false;
        }

        if (!IsValidAnalysisLevel(CSharpAnalysisLevel))
        {
            return false;
        }

        if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
        {
            return false;
        }

        if (RetryDelaySeconds < 1 || RetryDelaySeconds > 300)
        {
            return false;
        }

        if (HealthCheckIntervalSeconds < 10 || HealthCheckIntervalSeconds > 3600)
        {
            return false;
        }

        return true;
    }

    public override void ResetToDefaults()
    {
        EnableOrchestrator = true;
        OrchestratorMaxConcurrentTasks = 5;
        OrchestratorTimeoutSeconds = 300;
        EnableCSharpAgent = true;
        CSharpAnalysisLevel = "Full";
        EnableJavaScriptAgent = true;
        EnablePythonAgent = true;

        EnableRefactorAgent = true;
        EnableValidatorAgent = true;
        EnableKnowledgeAgent = true;
        EnableShellAgent = false;
        EnableTrainingDataGenerator = false;
        AutoRetryFailedTasks = true;
        MaxRetryAttempts = 3;
        RetryDelaySeconds = 5;
        EnableAgentHealthMonitoring = true;
        HealthCheckIntervalSeconds = 60;
    }

    private static bool IsValidAnalysisLevel(string level)
    {
        return level == "Basic" || level == "Full" || level == "Deep";
    }
}