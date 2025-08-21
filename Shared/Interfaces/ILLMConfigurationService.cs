
using A3sist.Shared.Models;
using System;
using System.Threading.Tasks;

namespace A3sist.Shared.Interfaces
{
    public class ILLMConfigurationService
    {
        public async Task<bool> ClearConfigurationAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<LLMConfiguration> GetConfigurationAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SaveApiKeyAsync(string apiKey, string provider)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> TestConnectionAsync(string apiKey, string provider)
        {
            throw new NotImplementedException();
        }
    }
}