using System.Threading.Tasks;

namespace A3sist.Orchastrator.LLM
{
    public interface ILLMClient
    {
        Task<bool> GetCompletionAsync(object prompt, object lLMOptions);
        Task<string> GetResponseAsync(string prompt);
    }
}