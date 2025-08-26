// File: FixerAgent.cs
using System.Threading.Tasks;
using A3sist.Shared.Interfaces;
using A3sist.Shared.Models;

namespace A3sist.Core.Agents.TaskAgents
{
    public class FixerAgent
    {
        private readonly ILLMClient _llmClient;

        public FixerAgent(ILLMClient llmClient)
        {
            _llmClient = llmClient;
        }

        public async Task<string> FixCodeAsync(string code)
        {
            var prompt = $"Fix the following code:\n{code}";
            var options = new LLMOptions { MaxTokens = 200, Temperature = 0.5f };

            return await _llmClient.GetCompletionAsync(prompt, options);
        }
    }
}