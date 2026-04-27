using MauiApp3.Models;

namespace MauiApp3.Services;

/// <summary>
/// Interface for managing chat communication (sending and receiving messages, files, and audio).
/// </summary>
public interface IChatService
{
    /// <summary>Event raised when a new message is received.</summary>
    event EventHandler<ChatMessage>? MessageReceived;

    /// <summary>Starts listening for incoming TCP connections on the specified port.</summary>
    Task StartListeningAsync(int port);
    
    /// <summary>Stops listening for incoming connections.</summary>
    Task StopListeningAsync();
    
    /// <summary>Sends a text message to a specific peer.</summary>
    Task SendMessageAsync(Peer peer, ChatMessage message);
    
    /// <summary>Broadcasts a text message to all active peers.</summary>
    Task BroadcastMessageAsync(ChatMessage message);
    
    /// <summary>Sends a file to a specific peer.</summary>
    Task SendFileAsync(Peer peer, string filePath, bool isGroupMessage);
    
    /// <summary>Broadcasts a file to all active peers.</summary>
    Task BroadcastFileAsync(string filePath);
    
    /// <summary>Sends an audio recording to a specific peer.</summary>
    Task SendAudioAsync(Peer peer, byte[] audioData, bool isGroupMessage);
    
    /// <summary>Broadcasts an audio recording to all active peers.</summary>
    Task BroadcastAudioAsync(byte[] audioData);
}
