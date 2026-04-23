using MauiApp3.Models;

namespace MauiApp3.Services;

public interface IChatService
{
    event EventHandler<ChatMessage>? MessageReceived;

    Task StartListeningAsync(int port);
    Task StopListeningAsync();
    Task SendMessageAsync(Peer peer, ChatMessage message);
    Task BroadcastMessageAsync(ChatMessage message);
    Task SendFileAsync(Peer peer, string filePath, bool isGroupMessage);
    Task BroadcastFileAsync(string filePath);
    Task SendAudioAsync(Peer peer, byte[] audioData, bool isGroupMessage);
    Task BroadcastAudioAsync(byte[] audioData);
}
