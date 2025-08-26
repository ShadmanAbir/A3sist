using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace A3sist.Chat.Desktop
{
    /// <summary>
    /// Simple chat service implementation that provides mock responses
    /// </summary>
    public class SimpleChatService : IChatService
    {
        private readonly ILogger<SimpleChatService> _logger;
        private readonly Random _random;

        public bool IsAvailable { get; private set; } = true;
        public event EventHandler<string>? StatusChanged;

        public SimpleChatService(ILogger<SimpleChatService> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        public async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Received message: {Message}", message);
            
            // Simulate processing delay
            await Task.Delay(_random.Next(1000, 3000), cancellationToken);
            
            // Generate a mock response
            var response = GenerateMockResponse(message);
            _logger.LogInformation("Generated response: {Response}", response);
            
            return response;
        }

        public async Task SendMessageStreamAsync(string message, Action<string> onChunk, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Received streaming message: {Message}", message);
            
            var response = GenerateMockResponse(message);
            var words = response.Split(' ');
            
            foreach (var word in words)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                onChunk(word + " ");
                await Task.Delay(_random.Next(50, 200), cancellationToken);
            }
        }

        private string GenerateMockResponse(string message)
        {
            var messageWords = message.ToLowerInvariant().Split(' ');
            
            if (Array.Exists(messageWords, w => w.Contains("code") || w.Contains("function") || w.Contains("method")))
            {
                return "I can help you with code! Here's a sample function:\n\n```csharp\npublic void ExampleMethod()\n{\n    Console.WriteLine(\"Hello from A3sist!\");\n}\n```\n\nThis is a simple example. In a real implementation, I would analyze your specific requirements and provide more targeted assistance.";
            }
            
            if (Array.Exists(messageWords, w => w.Contains("error") || w.Contains("bug") || w.Contains("fix")))
            {
                return "I'd be happy to help you debug that issue! To provide the best assistance, I would typically:\n\n1. Analyze your code structure\n2. Identify potential issues\n3. Suggest specific fixes\n4. Provide improved code examples\n\nFor now, this is a demo version running in standalone mode.";
            }
            
            if (Array.Exists(messageWords, w => w.Contains("hello") || w.Contains("hi") || w.Contains("hey")))
            {
                return "Hello! I'm A3sist, your AI-powered coding assistant. I'm currently running in standalone desktop mode.\n\nI can help you with:\n• Code analysis and suggestions\n• Debugging and error fixing\n• Code refactoring\n• General programming questions\n\nHow can I assist you today?";
            }
            
            if (Array.Exists(messageWords, w => w.Contains("help") || w.Contains("what") || w.Contains("how")))
            {
                return "I'm here to help! This is the A3sist Chat Desktop application running in standalone mode.\n\nKey features:\n• Interactive chat interface\n• Syntax highlighting\n• Code suggestions\n• Real-time assistance\n\nIn the full version, I would connect to advanced AI models and provide comprehensive coding assistance. What would you like to know more about?";
            }
            
            // Default response
            return $"Thank you for your message: \"{message}\"\n\nI'm currently running in demo mode. In the full version, I would provide intelligent responses based on:\n• Your code context\n• Project knowledge\n• Best practices\n• Real-time analysis\n\nThis standalone version demonstrates the chat interface and basic functionality. How else can I help you today?";
        }
    }
}