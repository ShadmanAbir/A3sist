// File: c:\Repo\A3sist\Orchastrator\LLM\ILLMClient.cs
using System.Threading.Tasks;

public interface ILLMClient
{
    // For simple text completion
    Task<LLMResponse> GetCompletionAsync(string prompt, LLMOptions options = null);

    // For chat-style conversations
    Task<LLMResponse> GetChatCompletionAsync(string[] messages, LLMOptions options = null);
}