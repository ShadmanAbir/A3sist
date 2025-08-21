// File: c:\Repo\A3sist\Orchastrator\LLM\ILLMClient.cs
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace A3sist.Orchastrator.LLM
{
    public interface ILLMClient
    {
        Task<bool> GetCompletionAsync(object prompt, object lLMOptions);
        Task<string> GetResponseAsync(string prompt);
    }

    public class CodestralLLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CodestralLLMClient> _logger;

        public CodestralLLMClient(ILogger<CodestralLLMClient> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public Task<bool> GetCompletionAsync(object prompt, object lLMOptions)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.codestral.com/v1/generate");
                request.Content = new StringContent(prompt, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Received response from LLM: {responseContent}");
                return responseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting response from LLM.");
                throw;
            }
        }
    }
}