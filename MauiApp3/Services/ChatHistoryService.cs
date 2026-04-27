using System.Collections.ObjectModel;
using MauiApp3.Models;

namespace MauiApp3.Services;

/// <summary>
/// Service responsible for storing and managing chat history locally during the app session.
/// </summary>
public class ChatHistoryService
{
    /// <summary>Collection of all group messages.</summary>
    public ObservableCollection<ChatMessage> GroupMessages { get; } = new();
    private readonly Dictionary<string, ObservableCollection<ChatMessage>> _privateMessages = new();

    /// <summary>
    /// Retrieves the private message history for a specific peer.
    /// Creates a new collection if one doesn't exist yet.
    /// </summary>
    /// <param name="peerIP">The IP address of the peer.</param>
    /// <returns>An ObservableCollection of chat messages.</returns>
    public ObservableCollection<ChatMessage> GetPrivateMessages(string peerIP)
    {
        if (!_privateMessages.TryGetValue(peerIP, out var messages))
        {
            messages = new ObservableCollection<ChatMessage>();
            _privateMessages[peerIP] = messages;
        }
        return messages;
    }

    /// <summary>
    /// Clears all stored chat history (group and private).
    /// </summary>
    public void ClearAll()
    {
        GroupMessages.Clear();
        _privateMessages.Clear();
    }
}
