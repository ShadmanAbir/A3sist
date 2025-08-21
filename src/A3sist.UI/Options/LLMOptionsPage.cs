using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace A3sist.UI.Options;

/// <summary>
/// LLM configuration options page
/// </summary>
[ComVisible(true)]
[Guid("12345678-1234-1234-1234-123456789014")]
public class LLMOptionsPage : BaseOptionsPage
{
    public override string CategoryName => "A3sist";
    public override string PageName => "LLM";

    [Category("Provider")]
    [DisplayName("LLM Provider")]
    [Description("The LLM provider to use (OpenAI, Azure, Anthropic, Local)")]
    public string Provider { get; set; } = "OpenAI";

    [Category("Provider")]
    [DisplayName("Model")]
    [Description("The specific model to use")]
    public string Model { get; set; } = "gpt-4";

    [Category("Provider")]
    [DisplayName("API Key")]
    [Description("API key for the LLM provider (stored securely)")]
    [PasswordPropertyText(true)]
    public string ApiKey { get; set; } = "";

    [Category("Provider")]
    [DisplayName("API Endpoint")]
    [Description("Custom API endpoint URL (optional)")]
    public string ApiEndpoint { get; set; } = "";

    [Category("Provider")]
    [DisplayName("Organization ID")]
    [Description("Organization ID for the LLM provider (optional)")]
    public string OrganizationId { get; set; } = "";

    [Category("Request Settings")]
    [DisplayName("Max Tokens")]
    [Description("Maximum number of tokens per request")]
    public int MaxTokens { get; set; } = 4000;

    [Category("Request Settings")]
    [DisplayName("Temperature")]
    [Description("Temperature for response generation (0.0 to 2.0)")]
    public double Temperature { get; set; } = 0.7;

    [Category("Request Settings")]
    [DisplayName("Top P")]
    [Description("Top P value for nucleus sampling (0.0 to 1.0)")]
    public double TopP { get; set; } = 1.0;

    [Category("Request Settings")]
    [DisplayName("Frequency Penalty")]
    [Description("Frequency penalty (-2.0 to 2.0)")]
    public double FrequencyPenalty { get; set; } = 0.0;

    [Category("Request Settings")]
    [DisplayName("Presence Penalty")]
    [Description("Presence penalty (-2.0 to 2.0)")]
    public double PresencePenalty { get; set; } = 0.0;

    [Category("Timeout & Retry")]
    [DisplayName("Request Timeout (seconds)")]
    [Description("Timeout for LLM requests in seconds")]
    public int RequestTimeoutSeconds { get; set; } = 60;

    [Category("Timeout & Retry")]
    [DisplayName("Max Retry Attempts")]
    [Description("Maximum number of retry attempts for failed requests")]
    public int MaxRetryAttempts { get; set; } = 3;

    [Category("Timeout & Retry")]
    [DisplayName("Retry Delay (seconds)")]
    [Description("Base delay between retry attempts in seconds")]
    public int RetryDelaySeconds { get; set; } = 2;

    [Category("Timeout & Retry")]
    [DisplayName("Max Retry Delay (seconds)")]
    [Description("Maximum delay between retry attempts in seconds")]
    public int MaxRetryDelaySeconds { get; set; } = 30;

    [Category("Rate Limiting")]
    [DisplayName("Enable Rate Limiting")]
    [Description("Enable rate limiting for LLM requests")]
    public bool EnableRateLimiting { get; set; } = true;

    [Category("Rate Limiting")]
    [DisplayName("Requests Per Minute")]
    [Description("Maximum number of requests per minute")]
    public int RequestsPerMinute { get; set; } = 60;

    [Category("Rate Limiting")]
    [DisplayName("Tokens Per Minute")]
    [Description("Maximum number of tokens per minute")]
    public int TokensPerMinute { get; set; } = 90000;

    [Category("Caching")]
    [DisplayName("Enable Response Caching")]
    [Description("Enable caching of LLM responses")]
    public bool EnableResponseCaching { get; set; } = true;

    [Category("Caching")]
    [DisplayName("Cache TTL (minutes)")]
    [Description("Time-to-live for cached responses in minutes")]
    public int CacheTTLMinutes { get; set; } = 60;

    [Category("Caching")]
    [DisplayName("Max Cache Size (MB)")]
    [Description("Maximum size of the response cache in MB")]
    public int MaxCacheSizeMB { get; set; } = 100;

    [Category("Privacy")]
    [DisplayName("Enable Data Anonymization")]
    [Description("Anonymize sensitive data before sending to LLM")]
    public bool EnableDataAnonymization { get; set; } = true;

    [Category("Privacy")]
    [DisplayName("Log Requests")]
    [Description("Log LLM requests for debugging (may contain sensitive data)")]
    public bool LogRequests { get; set; } = false;

    [Category("Privacy")]
    [DisplayName("Log Responses")]
    [Description("Log LLM responses for debugging")]
    public bool LogResponses { get; set; } = false;

    public override bool ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(Provider))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            return false;
        }

        if (MaxTokens < 1 || MaxTokens > 32000)
        {
            return false;
        }

        if (Temperature < 0.0 || Temperature > 2.0)
        {
            return false;
        }

        if (TopP < 0.0 || TopP > 1.0)
        {
            return false;
        }

        if (FrequencyPenalty < -2.0 || FrequencyPenalty > 2.0)
        {
            return false;
        }

        if (PresencePenalty < -2.0 || PresencePenalty > 2.0)
        {
            return false;
        }

        if (RequestTimeoutSeconds < 5 || RequestTimeoutSeconds > 600)
        {
            return false;
        }

        if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
        {
            return false;
        }

        if (RetryDelaySeconds < 1 || RetryDelaySeconds > 60)
        {
            return false;
        }

        if (MaxRetryDelaySeconds < RetryDelaySeconds || MaxRetryDelaySeconds > 300)
        {
            return false;
        }

        if (RequestsPerMinute < 1 || RequestsPerMinute > 1000)
        {
            return false;
        }

        if (TokensPerMinute < 1000 || TokensPerMinute > 1000000)
        {
            return false;
        }

        if (CacheTTLMinutes < 1 || CacheTTLMinutes > 1440)
        {
            return false;
        }

        if (MaxCacheSizeMB < 1 || MaxCacheSizeMB > 1000)
        {
            return false;
        }

        return true;
    }

    public override void ResetToDefaults()
    {
        Provider = "OpenAI";
        Model = "gpt-4";
        ApiKey = "";
        ApiEndpoint = "";
        OrganizationId = "";
        MaxTokens = 4000;
        Temperature = 0.7;
        TopP = 1.0;
        FrequencyPenalty = 0.0;
        PresencePenalty = 0.0;
        RequestTimeoutSeconds = 60;
        MaxRetryAttempts = 3;
        RetryDelaySeconds = 2;
        MaxRetryDelaySeconds = 30;
        EnableRateLimiting = true;
        RequestsPerMinute = 60;
        TokensPerMinute = 90000;
        EnableResponseCaching = true;
        CacheTTLMinutes = 60;
        MaxCacheSizeMB = 100;
        EnableDataAnonymization = true;
        LogRequests = false;
        LogResponses = false;
    }
}