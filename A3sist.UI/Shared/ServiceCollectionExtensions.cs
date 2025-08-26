using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.UI.Shared.Interfaces;
using A3sist.Core.Services;
using A3sist.Core.Agents.Core;
using A3sist.Core.LLM;

#if NET472
using A3sist.UI.Framework.VSIX;
#endif

#if NET9_0_OR_GREATER
using A3sist.UI.Framework.WPF;
using Microsoft.Extensions.Caching.Memory;
#endif

namespace A3sist.UI.Shared
{
    /// <summary>
    /// Service collection extensions for unified UI registration
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds A3sist UI services with framework-specific implementations
        /// </summary>
        public static IServiceCollection AddA3sistUI(this IServiceCollection services)
        {
            // Shared services (framework-agnostic)
            services.AddSingleton<EnhancedRequestRouter>();
            services.AddSingleton<RAGService>();
            services.AddSingleton<RAGEnhancedCSharpAgent>();
            services.AddSingleton<EnhancedMCPClient>();
            
            // Shared UI services
            services.AddTransient<ChatViewModel>();
            services.AddTransient<AgentStatusViewModel>();
            services.AddTransient<KnowledgeViewModel>();

            // Add HTTP client for RAG service
            services.AddHttpClient<RAGService>();
            services.AddHttpClient<EnhancedMCPClient>();

#if NET472
            // VSIX-specific services (.NET 4.7.2)
            services.AddSingleton<IUIService, VSIXUIService>();
            services.AddSingleton<IChatService, VSIXChatService>();
            services.AddSingleton<IRAGUIService>(provider => 
                (IRAGUIService)provider.GetRequiredService<IUIService>());
#endif

#if NET9_0_OR_GREATER
            // WPF-specific services (.NET 9)
            services.AddSingleton<IUIService, WPFUIService>();
            services.AddSingleton<IChatService, WPFChatService>();
            services.AddSingleton<IRAGUIService>(provider => 
                (IRAGUIService)provider.GetRequiredService<IUIService>());
            services.AddSingleton<IMemoryCache, MemoryCache>();
#endif

            return services;
        }

        /// <summary>
        /// Adds enhanced RAG services for knowledge-augmented functionality
        /// </summary>
        public static IServiceCollection AddRAGServices(this IServiceCollection services)
        {
            services.AddSingleton<RAGService>();
            services.AddHttpClient<RAGService>("knowledge", client =>
            {
                client.BaseAddress = new Uri("http://localhost:3003");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Add knowledge repository (would be implemented separately)
            // services.AddSingleton<IKnowledgeRepository, EmbeddingKnowledgeRepository>();

            return services;
        }

        /// <summary>
        /// Adds logging configuration optimized for each framework
        /// </summary>
        public static IServiceCollection AddFrameworkLogging(this IServiceCollection services)
        {
#if NET472
            // VSIX-compatible logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
                // Would add VS output window logger
            });
#endif

#if NET9_0_OR_GREATER
            // Modern .NET logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
                builder.AddDebug();
                builder.AddEventSourceLogger();
            });
#endif

            return services;
        }
    }

    /// <summary>
    /// Shared view model for chat functionality
    /// </summary>
    public class ChatViewModel
    {
        private readonly IChatService _chatService;
        private readonly IRAGUIService _ragUIService;
        private readonly ILogger<ChatViewModel> _logger;

        public ChatViewModel(
            IChatService chatService,
            IRAGUIService ragUIService,
            ILogger<ChatViewModel> logger)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _ragUIService = ragUIService ?? throw new ArgumentNullException(nameof(ragUIService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _chatService.MessageReceived += OnMessageReceived;
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                _logger.LogInformation("Sending message: {Message}", message);
                var response = await _chatService.SendMessageAsync(message);
                _logger.LogDebug("Received response: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
            }
        }

        public async Task ShowKnowledgeModeAsync()
        {
            await _ragUIService.ShowKnowledgeModeAsync();
        }

        private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            try
            {
                if (e.Citations?.Length > 0)
                {
                    await _ragUIService.ShowCitationsAsync(e.Citations);
                }

                if (e.RAGContext != null)
                {
                    await _ragUIService.UpdateKnowledgeStatusAsync(
                        $"Used {e.RAGContext.KnowledgeEntries.Count} knowledge sources");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling received message");
            }
        }
    }

    /// <summary>
    /// Shared view model for agent status
    /// </summary>
    public class AgentStatusViewModel
    {
        private readonly EnhancedRequestRouter _router;
        private readonly ILogger<AgentStatusViewModel> _logger;

        public AgentStatusViewModel(
            EnhancedRequestRouter router,
            ILogger<AgentStatusViewModel> logger)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public RoutingStatistics GetRoutingStatistics()
        {
            return _router.GetRoutingStatistics();
        }
    }

    /// <summary>
    /// Shared view model for knowledge functionality
    /// </summary>
    public class KnowledgeViewModel
    {
        private readonly RAGService _ragService;
        private readonly ILogger<KnowledgeViewModel> _logger;

        public KnowledgeViewModel(
            RAGService ragService,
            ILogger<KnowledgeViewModel> logger)
        {
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RAGContext> SearchKnowledgeAsync(string query)
        {
            try
            {
                var request = new AgentRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    Prompt = query
                };

                return await _ragService.RetrieveContextAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching knowledge");
                return new RAGContext();
            }
        }
    }
}