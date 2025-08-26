using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.UI.Editors;

namespace A3sist.UI.Services
{
    /// <summary>
    /// Service registration for editor integration services
    /// </summary>
    public static class EditorServiceRegistration
    {
        /// <summary>
        /// Registers all editor integration services with the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddEditorIntegrationServices(this IServiceCollection services)
        {
            // Register core editor services
            services.AddSingleton<CodeAnalysisProvider>();
            services.AddSingleton<SuggestionProvider>();
            services.AddSingleton<IEditorIntegrationService, EditorIntegrationService>();

            // Register service locator services (temporary solution)
            services.AddSingleton<IServiceLocator, ServiceLocatorImplementation>();

            return services;
        }

        /// <summary>
        /// Initializes the service locator with registered services
        /// </summary>
        /// <param name="serviceProvider">The service provider</param>
        public static void InitializeServiceLocator(IServiceProvider serviceProvider)
        {
            try
            {
                // Register services with the static service locator
                var suggestionService = serviceProvider.GetService<ISuggestionService>();
                if (suggestionService != null)
                {
                    ServiceLocator.RegisterService<ISuggestionService>(suggestionService);
                }

                var orchestrator = serviceProvider.GetService<IOrchestrator>();
                if (orchestrator != null)
                {
                    ServiceLocator.RegisterService<IOrchestrator>(orchestrator);
                }

                var codeAnalysisService = serviceProvider.GetService<ICodeAnalysisService>();
                if (codeAnalysisService != null)
                {
                    ServiceLocator.RegisterService<ICodeAnalysisService>(codeAnalysisService);
                }

                // Register loggers
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    ServiceLocator.RegisterService<ILogger<A3sistSuggestedActionsSource>>(
                        loggerFactory.CreateLogger<A3sistSuggestedActionsSource>());
                    ServiceLocator.RegisterService<ILogger<A3sistCompletionSource>>(
                        loggerFactory.CreateLogger<A3sistCompletionSource>());
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw to avoid breaking the extension
                var logger = serviceProvider.GetService<ILogger<EditorServiceRegistration>>();
                logger?.LogError(ex, "Error initializing service locator");
            }
        }
    }

    /// <summary>
    /// Interface for service locator (for dependency injection)
    /// </summary>
    public interface IServiceLocator
    {
        T GetService<T>();
        void RegisterService<T>(T service);
    }

    /// <summary>
    /// Implementation of service locator using DI container
    /// </summary>
    internal class ServiceLocatorImplementation : IServiceLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceLocatorImplementation(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        public void RegisterService<T>(T service)
        {
            // In a real implementation, this would register with the DI container
            // For now, we'll use the static service locator
            ServiceLocator.RegisterService<T>(service);
        }
    }
}