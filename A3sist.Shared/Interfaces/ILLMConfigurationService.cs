﻿
using A3sist.Shared.Models;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public interface ILLMConfigurationService
    {
        Task<bool> ClearConfigurationAsync();
        Task<LLMConfiguration> GetConfigurationAsync();
        Task<bool> SaveApiKeyAsync(string apiKey, string provider);
        Task<bool> TestConnectionAsync(string apiKey, string provider);
    }
}