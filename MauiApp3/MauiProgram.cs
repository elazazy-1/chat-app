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
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton<ChatHistoryService>();
            builder.Services.AddSingleton<ILanDiscoveryService, LanDiscoveryService>();
            builder.Services.AddSingleton<IChatService, ChatService>();
            builder.Services.AddSingleton<IAudioRecorderService, AudioRecorderService>();

            // ViewModels
            builder.Services.AddTransient<JoinViewModel>();
            builder.Services.AddSingleton<LobbyViewModel>();
            builder.Services.AddSingleton<GroupChatViewModel>();
            builder.Services.AddTransient<PrivateChatViewModel>();

            // Pages
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
