using System.Collections.ObjectModel;
using MauiApp3.Models;

namespace MauiApp3.Services;

public class ChatHistoryService
{
    public ObservableCollection<ChatMessage> GroupMessages { get; } = new();
    private readonly Dictionary<string, ObservableCollection<ChatMessage>> _privateMessages = new();

    public ObservableCollection<ChatMessage> GetPrivateMessages(string peerIP)
    {
        if (!_privateMessages.TryGetValue(peerIP, out var messages))
        {
            messages = new ObservableCollection<ChatMessage>();
            _privateMessages[peerIP] = messages;
        }
        return messages;
    }

    public void ClearAll()
    {
        GroupMessages.Clear();
        _privateMessages.Clear();
    }
}
