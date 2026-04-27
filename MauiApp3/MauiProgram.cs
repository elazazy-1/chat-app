using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using MauiApp3.Features.Chat.Group;
using MauiApp3.Features.Chat.Private;
using MauiApp3.Features.Join;
using MauiApp3.Features.Lobby;
using MauiApp3.Services;

namespace MauiApp3
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    // Register custom fonts so they can be referenced globally in XAML
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services: Registered as Singletons to share state across the entire application lifecycle.
            builder.Services.AddSingleton(AudioManager.Current); // Plugin.Maui.Audio
            builder.Services.AddSingleton<ChatHistoryService>(); // Stores message lists
            builder.Services.AddSingleton<ILanDiscoveryService, LanDiscoveryService>(); // UDP Network
            builder.Services.AddSingleton<IChatService, ChatService>(); // TCP Network
            builder.Services.AddSingleton<IAudioRecorderService, AudioRecorderService>(); // Audio Capture

            // ViewModels: Defines whether states should persist or recreate.
            // Transients are recreated every time the page opens. Singletons retain their state.
            builder.Services.AddTransient<JoinViewModel>();
            builder.Services.AddSingleton<LobbyViewModel>(); // Maintain peer list state globally
            builder.Services.AddSingleton<GroupChatViewModel>(); // Maintain group chat context
            builder.Services.AddTransient<PrivateChatViewModel>(); // Recreate for different peers

            // Pages: Corresponding UI views for the ViewModels
            builder.Services.AddTransient<JoinPage>();
            builder.Services.AddSingleton<LobbyPage>();
            builder.Services.AddTransient<GroupChatPage>();
            builder.Services.AddTransient<PrivateChatPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
