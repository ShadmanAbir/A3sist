using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Text.Json;
using System.Windows.Forms;

namespace A3sist.UI.Options;

/// <summary>
/// Utility class for importing and exporting A3sist configuration
/// </summary>
public static class ConfigurationImportExport
{
    /// <summary>
    /// Exports the current configuration to a JSON file
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>True if export was successful, false otherwise</returns>
    public static bool ExportConfiguration(IServiceProvider serviceProvider)
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Export A3sist Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"A3sist_Config_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            var configuration = GatherCurrentConfiguration(serviceProvider);
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(saveFileDialog.FileName, json);

            ShowMessage(serviceProvider, "Configuration exported successfully.", "Export Complete", 
                OLEMSGICON.OLEMSGICON_INFO);

            return true;
        }
        catch (Exception ex)
        {
            ShowMessage(serviceProvider, $"Failed to export configuration: {ex.Message}", "Export Failed", 
                OLEMSGICON.OLEMSGICON_CRITICAL);
            return false;
        }
    }

    /// <summary>
    /// Imports configuration from a JSON file
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>True if import was successful, false otherwise</returns>
    public static bool ImportConfiguration(IServiceProvider serviceProvider)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Import A3sist Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            var json = File.ReadAllText(openFileDialog.FileName);
            var configuration = JsonSerializer.Deserialize<A3sistConfigurationExport>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (configuration == null)
            {
                ShowMessage(serviceProvider, "Invalid configuration file format.", "Import Failed", 
                    OLEMSGICON.OLEMSGICON_CRITICAL);
                return false;
            }

            // Confirm import
            var result = ShowMessage(serviceProvider, 
                "This will overwrite your current A3sist configuration. Do you want to continue?", 
                "Confirm Import", OLEMSGICON.OLEMSGICON_QUESTION, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);

            if (result != 6) // IDYES
            {
                return false;
            }

            ApplyImportedConfiguration(serviceProvider, configuration);

            ShowMessage(serviceProvider, "Configuration imported successfully. Please restart Visual Studio for all changes to take effect.", 
                "Import Complete", OLEMSGICON.OLEMSGICON_INFO);

            return true;
        }
        catch (Exception ex)
        {
            ShowMessage(serviceProvider, $"Failed to import configuration: {ex.Message}", "Import Failed", 
                OLEMSGICON.OLEMSGICON_CRITICAL);
            return false;
        }
    }

    /// <summary>
    /// Resets all configuration to default values
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>True if reset was successful, false otherwise</returns>
    public static bool ResetToDefaults(IServiceProvider serviceProvider)
    {
        try
        {
            // Confirm reset
            var result = ShowMessage(serviceProvider, 
                "This will reset all A3sist configuration to default values. Do you want to continue?", 
                "Confirm Reset", OLEMSGICON.OLEMSGICON_QUESTION, OLEMSGBUTTON.OLEMSGBUTTON_YESNO);

            if (result != 6) // IDYES
            {
                return false;
            }

            // Reset all options pages
            ResetOptionsPage<GeneralOptionsPage>(serviceProvider);
            ResetOptionsPage<AgentOptionsPage>(serviceProvider);
            ResetOptionsPage<LLMOptionsPage>(serviceProvider);

            ShowMessage(serviceProvider, "Configuration reset to defaults successfully.", "Reset Complete", 
                OLEMSGICON.OLEMSGICON_INFO);

            return true;
        }
        catch (Exception ex)
        {
            ShowMessage(serviceProvider, $"Failed to reset configuration: {ex.Message}", "Reset Failed", 
                OLEMSGICON.OLEMSGICON_CRITICAL);
            return false;
        }
    }

    private static A3sistConfigurationExport GatherCurrentConfiguration(IServiceProvider serviceProvider)
    {
        var generalOptions = GetOptionsPage<GeneralOptionsPage>(serviceProvider);
        var agentOptions = GetOptionsPage<AgentOptionsPage>(serviceProvider);
        var llmOptions = GetOptionsPage<LLMOptionsPage>(serviceProvider);

        return new A3sistConfigurationExport
        {
            General = new GeneralConfigurationExport
            {
                EnableA3sist = generalOptions.EnableA3sist,
                AutoStartOnVSStartup = generalOptions.AutoStartOnVSStartup,
                ShowNotifications = generalOptions.ShowNotifications,
                EnableTelemetry = generalOptions.EnableTelemetry,
                MaxConcurrentTasks = generalOptions.MaxConcurrentTasks,
                TaskTimeoutSeconds = generalOptions.TaskTimeoutSeconds,
                EnableCaching = generalOptions.EnableCaching,
                CacheExpiryMinutes = generalOptions.CacheExpiryMinutes,
                LogLevel = generalOptions.LogLevel.ToString(),
                EnableFileLogging = generalOptions.EnableFileLogging,
                LogFilePath = generalOptions.LogFilePath,
                MaxLogFileSizeMB = generalOptions.MaxLogFileSizeMB,
                MaxLogFiles = generalOptions.MaxLogFiles
            },
            Agents = new AgentConfigurationExport
            {
                EnableOrchestrator = agentOptions.EnableOrchestrator,
                OrchestratorMaxConcurrentTasks = agentOptions.OrchestratorMaxConcurrentTasks,
                OrchestratorTimeoutSeconds = agentOptions.OrchestratorTimeoutSeconds,
                EnableCSharpAgent = agentOptions.EnableCSharpAgent,
                CSharpAnalysisLevel = agentOptions.CSharpAnalysisLevel,
                EnableJavaScriptAgent = agentOptions.EnableJavaScriptAgent,
                EnablePythonAgent = agentOptions.EnablePythonAgent,
                EnableFixerAgent = agentOptions.EnableFixerAgent,
                EnableRefactorAgent = agentOptions.EnableRefactorAgent,
                EnableValidatorAgent = agentOptions.EnableValidatorAgent,
                EnableKnowledgeAgent = agentOptions.EnableKnowledgeAgent,
                EnableShellAgent = agentOptions.EnableShellAgent,
                EnableTrainingDataGenerator = agentOptions.EnableTrainingDataGenerator,
                AutoRetryFailedTasks = agentOptions.AutoRetryFailedTasks,
                MaxRetryAttempts = agentOptions.MaxRetryAttempts,
                RetryDelaySeconds = agentOptions.RetryDelaySeconds,
                EnableAgentHealthMonitoring = agentOptions.EnableAgentHealthMonitoring,
                HealthCheckIntervalSeconds = agentOptions.HealthCheckIntervalSeconds
            },
            LLM = new LLMConfigurationExport
            {
                Provider = llmOptions.Provider,
                Model = llmOptions.Model,
                ApiEndpoint = llmOptions.ApiEndpoint,
                OrganizationId = llmOptions.OrganizationId,
                MaxTokens = llmOptions.MaxTokens,
                Temperature = llmOptions.Temperature,
                TopP = llmOptions.TopP,
                FrequencyPenalty = llmOptions.FrequencyPenalty,
                PresencePenalty = llmOptions.PresencePenalty,
                RequestTimeoutSeconds = llmOptions.RequestTimeoutSeconds,
                MaxRetryAttempts = llmOptions.MaxRetryAttempts,
                RetryDelaySeconds = llmOptions.RetryDelaySeconds,
                MaxRetryDelaySeconds = llmOptions.MaxRetryDelaySeconds,
                EnableRateLimiting = llmOptions.EnableRateLimiting,
                RequestsPerMinute = llmOptions.RequestsPerMinute,
                TokensPerMinute = llmOptions.TokensPerMinute,
                EnableResponseCaching = llmOptions.EnableResponseCaching,
                CacheTTLMinutes = llmOptions.CacheTTLMinutes,
                MaxCacheSizeMB = llmOptions.MaxCacheSizeMB,
                EnableDataAnonymization = llmOptions.EnableDataAnonymization,
                LogRequests = llmOptions.LogRequests,
                LogResponses = llmOptions.LogResponses
            },
            ExportedAt = DateTime.UtcNow,
            Version = "1.0"
        };
    }

    private static void ApplyImportedConfiguration(IServiceProvider serviceProvider, A3sistConfigurationExport configuration)
    {
        // Apply general configuration
        if (configuration.General != null)
        {
            var generalOptions = GetOptionsPage<GeneralOptionsPage>(serviceProvider);
            generalOptions.EnableA3sist = configuration.General.EnableA3sist;
            generalOptions.AutoStartOnVSStartup = configuration.General.AutoStartOnVSStartup;
            generalOptions.ShowNotifications = configuration.General.ShowNotifications;
            generalOptions.EnableTelemetry = configuration.General.EnableTelemetry;
            generalOptions.MaxConcurrentTasks = configuration.General.MaxConcurrentTasks;
            generalOptions.TaskTimeoutSeconds = configuration.General.TaskTimeoutSeconds;
            generalOptions.EnableCaching = configuration.General.EnableCaching;
            generalOptions.CacheExpiryMinutes = configuration.General.CacheExpiryMinutes;
            if (Enum.TryParse<LogLevel>(configuration.General.LogLevel, out var logLevel))
            {
                generalOptions.LogLevel = logLevel;
            }
            generalOptions.EnableFileLogging = configuration.General.EnableFileLogging;
            generalOptions.LogFilePath = configuration.General.LogFilePath;
            generalOptions.MaxLogFileSizeMB = configuration.General.MaxLogFileSizeMB;
            generalOptions.MaxLogFiles = configuration.General.MaxLogFiles;
            generalOptions.SaveSettingsToStorage();
        }

        // Apply agent configuration
        if (configuration.Agents != null)
        {
            var agentOptions = GetOptionsPage<AgentOptionsPage>(serviceProvider);
            agentOptions.EnableOrchestrator = configuration.Agents.EnableOrchestrator;
            agentOptions.OrchestratorMaxConcurrentTasks = configuration.Agents.OrchestratorMaxConcurrentTasks;
            agentOptions.OrchestratorTimeoutSeconds = configuration.Agents.OrchestratorTimeoutSeconds;
            agentOptions.EnableCSharpAgent = configuration.Agents.EnableCSharpAgent;
            agentOptions.CSharpAnalysisLevel = configuration.Agents.CSharpAnalysisLevel;
            agentOptions.EnableJavaScriptAgent = configuration.Agents.EnableJavaScriptAgent;
            agentOptions.EnablePythonAgent = configuration.Agents.EnablePythonAgent;
            agentOptions.EnableFixerAgent = configuration.Agents.EnableFixerAgent;
            agentOptions.EnableRefactorAgent = configuration.Agents.EnableRefactorAgent;
            agentOptions.EnableValidatorAgent = configuration.Agents.EnableValidatorAgent;
            agentOptions.EnableKnowledgeAgent = configuration.Agents.EnableKnowledgeAgent;
            agentOptions.EnableShellAgent = configuration.Agents.EnableShellAgent;
            agentOptions.EnableTrainingDataGenerator = configuration.Agents.EnableTrainingDataGenerator;
            agentOptions.AutoRetryFailedTasks = configuration.Agents.AutoRetryFailedTasks;
            agentOptions.MaxRetryAttempts = configuration.Agents.MaxRetryAttempts;
            agentOptions.RetryDelaySeconds = configuration.Agents.RetryDelaySeconds;
            agentOptions.EnableAgentHealthMonitoring = configuration.Agents.EnableAgentHealthMonitoring;
            agentOptions.HealthCheckIntervalSeconds = configuration.Agents.HealthCheckIntervalSeconds;
            agentOptions.SaveSettingsToStorage();
        }

        // Apply LLM configuration (excluding sensitive data like API key)
        if (configuration.LLM != null)
        {
            var llmOptions = GetOptionsPage<LLMOptionsPage>(serviceProvider);
            llmOptions.Provider = configuration.LLM.Provider;
            llmOptions.Model = configuration.LLM.Model;
            llmOptions.ApiEndpoint = configuration.LLM.ApiEndpoint;
            llmOptions.OrganizationId = configuration.LLM.OrganizationId;
            llmOptions.MaxTokens = configuration.LLM.MaxTokens;
            llmOptions.Temperature = configuration.LLM.Temperature;
            llmOptions.TopP = configuration.LLM.TopP;
            llmOptions.FrequencyPenalty = configuration.LLM.FrequencyPenalty;
            llmOptions.PresencePenalty = configuration.LLM.PresencePenalty;
            llmOptions.RequestTimeoutSeconds = configuration.LLM.RequestTimeoutSeconds;
            llmOptions.MaxRetryAttempts = configuration.LLM.MaxRetryAttempts;
            llmOptions.RetryDelaySeconds = configuration.LLM.RetryDelaySeconds;
            llmOptions.MaxRetryDelaySeconds = configuration.LLM.MaxRetryDelaySeconds;
            llmOptions.EnableRateLimiting = configuration.LLM.EnableRateLimiting;
            llmOptions.RequestsPerMinute = configuration.LLM.RequestsPerMinute;
            llmOptions.TokensPerMinute = configuration.LLM.TokensPerMinute;
            llmOptions.EnableResponseCaching = configuration.LLM.EnableResponseCaching;
            llmOptions.CacheTTLMinutes = configuration.LLM.CacheTTLMinutes;
            llmOptions.MaxCacheSizeMB = configuration.LLM.MaxCacheSizeMB;
            llmOptions.EnableDataAnonymization = configuration.LLM.EnableDataAnonymization;
            llmOptions.LogRequests = configuration.LLM.LogRequests;
            llmOptions.LogResponses = configuration.LLM.LogResponses;
            llmOptions.SaveSettingsToStorage();
        }
    }

    private static T GetOptionsPage<T>(IServiceProvider serviceProvider) where T : DialogPage, new()
    {
        if (serviceProvider.GetService(typeof(Package)) is Package package)
        {
            return (T)package.GetDialogPage(typeof(T));
        }
        return new T();
    }

    private static void ResetOptionsPage<T>(IServiceProvider serviceProvider) where T : BaseOptionsPage, new()
    {
        var optionsPage = GetOptionsPage<T>(serviceProvider);
        optionsPage.ResetToDefaults();
        optionsPage.SaveSettingsToStorage();
    }

    private static int ShowMessage(IServiceProvider serviceProvider, string message, string title, 
        OLEMSGICON icon, OLEMSGBUTTON button = OLEMSGBUTTON.OLEMSGBUTTON_OK)
    {
        var uiShell = serviceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell;
        var clsid = Guid.Empty;
        uiShell?.ShowMessageBox(0, ref clsid, title, message, string.Empty, 0, button, 
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, icon, 0, out var result);
        return result;
    }
}

// Export data models
public class A3sistConfigurationExport
{
    public GeneralConfigurationExport General { get; set; }
    public AgentConfigurationExport Agents { get; set; }
    public LLMConfigurationExport LLM { get; set; }
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; }
}

public class GeneralConfigurationExport
{
    public bool EnableA3sist { get; set; }
    public bool AutoStartOnVSStartup { get; set; }
    public bool ShowNotifications { get; set; }
    public bool EnableTelemetry { get; set; }
    public int MaxConcurrentTasks { get; set; }
    public int TaskTimeoutSeconds { get; set; }
    public bool EnableCaching { get; set; }
    public int CacheExpiryMinutes { get; set; }
    public string LogLevel { get; set; }
    public bool EnableFileLogging { get; set; }
    public string LogFilePath { get; set; }
    public int MaxLogFileSizeMB { get; set; }
    public int MaxLogFiles { get; set; }
}

public class AgentConfigurationExport
{
    public bool EnableOrchestrator { get; set; }
    public int OrchestratorMaxConcurrentTasks { get; set; }
    public int OrchestratorTimeoutSeconds { get; set; }
    public bool EnableCSharpAgent { get; set; }
    public string CSharpAnalysisLevel { get; set; }
    public bool EnableJavaScriptAgent { get; set; }
    public bool EnablePythonAgent { get; set; }
    public bool EnableFixerAgent { get; set; }
    public bool EnableRefactorAgent { get; set; }
    public bool EnableValidatorAgent { get; set; }
    public bool EnableKnowledgeAgent { get; set; }
    public bool EnableShellAgent { get; set; }
    public bool EnableTrainingDataGenerator { get; set; }
    public bool AutoRetryFailedTasks { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public bool EnableAgentHealthMonitoring { get; set; }
    public int HealthCheckIntervalSeconds { get; set; }
}

public class LLMConfigurationExport
{
    public string Provider { get; set; }
    public string Model { get; set; }
    // Note: API Key is intentionally excluded for security
    public string ApiEndpoint { get; set; }
    public string OrganizationId { get; set; }
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
    public double TopP { get; set; }
    public double FrequencyPenalty { get; set; }
    public double PresencePenalty { get; set; }
    public int RequestTimeoutSeconds { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public int MaxRetryDelaySeconds { get; set; }
    public bool EnableRateLimiting { get; set; }
    public int RequestsPerMinute { get; set; }
    public int TokensPerMinute { get; set; }
    public bool EnableResponseCaching { get; set; }
    public int CacheTTLMinutes { get; set; }
    public int MaxCacheSizeMB { get; set; }
    public bool EnableDataAnonymization { get; set; }
    public bool LogRequests { get; set; }
    public bool LogResponses { get; set; }
}