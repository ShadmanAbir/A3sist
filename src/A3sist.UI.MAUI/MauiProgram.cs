using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace A3sist.UI.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register ViewModels and Services
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<AgentStatusPage>();
        
        // Register Core Services
        builder.Services.AddSingleton<IChatService, ChatService>();
        builder.Services.AddSingleton<IAgentStatusService, AgentStatusService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}