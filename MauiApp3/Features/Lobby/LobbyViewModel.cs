using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Features.Chat.Group;
using MauiApp3.Features.Chat.Private;
using MauiApp3.Models;
using MauiApp3.Services;

namespace MauiApp3.Features.Lobby;

public class LobbyViewModel : INotifyPropertyChanged
{
    private readonly ILanDiscoveryService _discoveryService;
    private readonly IChatService _chatService;
    private int _groupUnreadCount;

    public ObservableCollection<Peer> OnlinePeers { get; } = new();

    public int GroupUnreadCount
    {
        get => _groupUnreadCount;
        set => SetProperty(ref _groupUnreadCount, value);
    }

    public string DisplayName => _discoveryService.DisplayName;
    public string LocalIP => _discoveryService.LocalIP;

    public ICommand OpenGroupChatCommand { get; }
    public ICommand OpenPrivateChatCommand { get; }
    public ICommand RefreshCommand { get; }

    public LobbyViewModel(ILanDiscoveryService discoveryService, IChatService chatService)
    {
        _discoveryService = discoveryService;
        _chatService = chatService;

        OpenGroupChatCommand = new Command(async () => await Shell.Current.GoToAsync("GroupChatPage"));
        OpenPrivateChatCommand = new Command<Peer>(async (peer) =>
        {
            if (peer != null)
            {
                peer.UnreadCount = 0;
                await Shell.Current.GoToAsync($"PrivateChatPage?peerIP={peer.IPAddress}&peerName={peer.Name}");
            }
        });
        RefreshCommand = new Command(RefreshPeers);

        _discoveryService.PeersUpdated += OnPeersUpdated;
        _discoveryService.PeerDiscovered += OnPeerDiscovered;
        _discoveryService.PeerLost += OnPeerLost;
        _chatService.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(object? sender, ChatMessage msg)
    {
        if (msg.IsMine) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (msg.IsGroupMessage)
            {
                // Only increment if user is not currently on the group chat page
                if (Shell.Current.CurrentPage is not GroupChatPage)
                    GroupUnreadCount++;
            }
            else
            {
                var peer = OnlinePeers.FirstOrDefault(p => p.IPAddress == msg.SenderIP);
                if (peer != null)
                {
                    // Only increment if user is not currently on this peer's private chat page
                    if (Shell.Current.CurrentPage is PrivateChatPage privatePage &&
                        privatePage.BindingContext is PrivateChatViewModel vm &&
                        vm.PeerIP == peer.IPAddress)
                    {
                        // User is already viewing this chat; don't count as unread
                        return;
                    }
                    peer.UnreadCount++;
                }
            }
        });
    }

    private void OnPeerDiscovered(object? sender, Peer peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!OnlinePeers.Any(p => p.IPAddress == peer.IPAddress))
            {
                OnlinePeers.Add(peer);
            }
            OnPropertyChanged(nameof(OnlinePeers));
        });
    }

    private void OnPeerLost(object? sender, Peer peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = OnlinePeers.FirstOrDefault(p => p.IPAddress == peer.IPAddress);
            if (existing != null)
            {
                OnlinePeers.Remove(existing);
            }
            OnPropertyChanged(nameof(OnlinePeers));
        });
    }

    private void OnPeersUpdated(object? sender, List<Peer> peers)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Sync list
            var currentIPs = OnlinePeers.Select(p => p.IPAddress).ToHashSet();
            var newIPs = peers.Select(p => p.IPAddress).ToHashSet();

            // Remove departed
            var toRemove = OnlinePeers.Where(p => !newIPs.Contains(p.IPAddress)).ToList();
            foreach (var p in toRemove) OnlinePeers.Remove(p);

            // Add new
            foreach (var p in peers)
            {
                if (!currentIPs.Contains(p.IPAddress))
                    OnlinePeers.Add(p);
            }
        });
    }

    private void RefreshPeers()
    {
        var peers = _discoveryService.GetActivePeers();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnlinePeers.Clear();
            foreach (var p in peers) OnlinePeers.Add(p);
        });
    }

    public void ResetGroupUnread() => GroupUnreadCount = 0;

    public void ResetPeerUnread(string peerIP)
    {
        var peer = OnlinePeers.FirstOrDefault(p => p.IPAddress == peerIP);
        if (peer != null)
            peer.UnreadCount = 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
