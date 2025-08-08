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
}