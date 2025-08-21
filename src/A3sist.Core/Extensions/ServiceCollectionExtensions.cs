using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Extensions.Logging;
using A3sist.Shared.Interfaces;
using A3sist.Core.Services;
using A3sist.Core.Services.WorkflowSteps;
using A3sist.Core.Configuration;
using System.IO;

namespace A3sist.Core.Extensions;

/// <summary>
/// Extension methods for configuring services in the dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all A3sist services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddA3sistServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<A3sistConfiguration>(configuration.GetSection(A3sistConfiguration.SectionName));

        // Add logging
        services.AddA3sistLogging(configuration);

        // Add core services
        services.AddCoreServices(configuration);
        
        // Add agent services
        services.AddAgentServices(configuration);
        
        // Add LLM services
        services.AddLLMServices(configuration);

        return services;
    }

    /// <summary>
    /// Adds logging services with Serilog configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddA3sistLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext();

            // Ensure log directory exists
            var logPath = Path.Combine(Path.GetTempPath(), "A3sist", "logs");
            Directory.CreateDirectory(logPath);

            var logger = loggerConfig.CreateLogger();
            builder.AddSerilog(logger);
        });

        return services;
    }

    /// <summary>
    /// Adds core services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration providers
        services.AddConfigurationProviders(configuration);
        
        // Register configuration service
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Register settings persistence service
        services.AddSingleton<ISettingsPersistenceService, SettingsPersistenceService>();
        
        // Register agent status service
        services.AddSingleton<IAgentStatusService, AgentStatusService>();
        
        // Register agent manager
        services.AddSingleton<IAgentManager, AgentManager>();
        
        // Register orchestrator
        services.AddSingleton<IOrchestrator, Orchestrator>();
        
        // Register task queue service
        services.AddSingleton<ITaskQueueService, TaskQueueService>();
        
        // Register workflow service
        services.AddSingleton<IWorkflowService, WorkflowService>();
        
        // Register task queue processor
        services.AddHostedService<TaskQueueProcessor>();
        
        // Register workflow steps
        services.AddSingleton<ValidationWorkflowStep>();
        services.AddSingleton<PreprocessingWorkflowStep>();
        
        // Register intent classification services
        services.AddSingleton<IIntentClassifier, IntentClassifier>();
        services.AddSingleton<IRoutingRuleService, RoutingRuleService>();
        
        return services;
    }

    /// <summary>
    /// Adds configuration providers to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConfigurationProviders(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration providers
        services.AddSingleton<A3sist.Shared.Interfaces.IConfigurationProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<A3sist.Core.Configuration.Providers.FileConfigurationProvider>>();
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "A3sist", "config.json");
            return new A3sist.Core.Configuration.Providers.FileConfigurationProvider(configPath, logger);
        });

        services.AddSingleton<A3sist.Shared.Interfaces.IConfigurationProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<A3sist.Core.Configuration.Providers.RegistryConfigurationProvider>>();
            return new A3sist.Core.Configuration.Providers.RegistryConfigurationProvider(@"SOFTWARE\A3sist", logger);
        });

        services.AddSingleton<A3sist.Shared.Interfaces.IConfigurationProvider>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<A3sist.Core.Configuration.Providers.EnvironmentConfigurationProvider>>();
            return new A3sist.Core.Configuration.Providers.EnvironmentConfigurationProvider("A3SIST", logger);
        });

        return services;
    }

    /// <summary>
    /// Adds agent services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core agents
        services.AddTransient<A3sist.Core.Agents.Core.IntentRouterAgent>();
        
        // Agent services will be registered here in subsequent tasks
        // This includes:
        // - Base agent infrastructure
        // - Language-specific agents
        // - Task-specific agents
        // - Utility agents
        
        return services;
    }

    /// <summary>
    /// Adds LLM services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddLLMServices(this IServiceCollection services, IConfiguration configuration)
    {
        // HTTP client for LLM services
        services.AddHttpClient();
        
        // LLM services will be registered here in subsequent tasks
        // This includes:
        // - ILLMClient implementations
        // - LLM cache service
        // - LLM retry policies
        
        return services;
    }

    /// <summary>
    /// Adds UI services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddUIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // UI services will be registered here in subsequent tasks
        // This includes:
        // - INotificationService
        // - ISuggestionService
        // - IEditorIntegrationService
        
        return services;
    }

    /// <summary>
    /// Validates the service registration and configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection ValidateA3sistServices(this IServiceCollection services)
    {
        // Build a temporary service provider to validate registrations
        using var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            // Validate core services can be resolved
            serviceProvider.GetRequiredService<IConfiguration>();
            serviceProvider.GetRequiredService<IConfigurationService>();
            serviceProvider.GetRequiredService<ILoggerFactory>();
            
            // Additional validation will be added in subsequent tasks
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Service registration validation failed", ex);
        }

        return services;
    }
}