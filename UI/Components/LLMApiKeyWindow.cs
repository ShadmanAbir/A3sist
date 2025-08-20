using System;
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Messaging;
using Microsoft.Extensions.Logging;

namespace A3sist.UI.Components
{
    public class LLMApiKeyWindow
    {
        private readonly ILLMConfigurationService _configurationService;
        private readonly ILogger<LLMApiKeyWindow> _logger;

        public LLMApiKeyWindow(
            ILLMConfigurationService configurationService,
            ILogger<LLMApiKeyWindow> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        public async Task<LLMConfiguration> GetCurrentConfigurationAsync()
        {
            try
            {
                var config = await _configurationService.GetConfigurationAsync();
                _logger.LogInformation("Retrieved current LLM configuration");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving LLM configuration");
                return new LLMConfiguration();
            }
        }

        public async Task<bool> SaveApiKeyAsync(string apiKey, string provider)
        {
            try
            {
                var result = await _configurationService.SaveApiKeyAsync(apiKey, provider);
                if (result)
                {
                    _logger.LogInformation($"Successfully saved API key for provider: {provider}");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving API key for provider: {provider}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(string apiKey, string provider)
        {
            try
            {
                var result = await _configurationService.TestConnectionAsync(apiKey, provider);
                if (result)
                {
                    _logger.LogInformation($"Connection test successful for provider: {provider}");
                }
                else
                {
                    _logger.LogWarning($"Connection test failed for provider: {provider}");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing connection for provider: {provider}");
                return false;
            }
        }

        public async Task<bool> ClearConfigurationAsync()
        {
            try
            {
                var result = await _configurationService.ClearConfigurationAsync();
                if (result)
                {
                    _logger.LogInformation("Successfully cleared LLM configuration");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing LLM configuration");
                return false;
            }
        }
    }
}