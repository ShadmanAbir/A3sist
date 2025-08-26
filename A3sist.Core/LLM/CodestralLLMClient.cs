using A3sist.Orchastrator.LLM;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class CodestralLLMClient : ILLMClient
{
    private readonly HttpClient _httpClient;

    public CodestralLLMClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetCompletionAsync(string prompt, LLMOptions options = null)
    {
        options ??= new LLMOptions();

        var requestBody = new
        {
            prompt,
            max_tokens = options.MaxTokens,
            temperature = options.Temperature,
            stop = options.Stop
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

        return responseObject.GetProperty("completion").GetString();
    }

    public async Task<bool> GetCompletionAsync(object prompt, object lLMOptions)
    {
        try
        {
            if (prompt == null)
                return false;

            var promptString = prompt.ToString();
            var response = await GetCompletionAsync(promptString, lLMOptions as LLMOptions);
            return !string.IsNullOrEmpty(response);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<string> GetResponseAsync(string prompt)
    {
        try
        {
            // Use the existing GetCompletionAsync method
            return await GetCompletionAsync(prompt, new LLMOptions());
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}