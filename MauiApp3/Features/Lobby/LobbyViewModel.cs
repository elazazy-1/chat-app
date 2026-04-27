using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Features.Chat.Group;
using MauiApp3.Features.Chat.Private;
using MauiApp3.Models;
using MauiApp3.Services;

namespace MauiApp3.Features.Lobby;

/// <summary>
/// ViewModel for the Lobby screen, showing the list of active peers
/// and handling navigation to group or private chats.
/// </summary>
public class LobbyViewModel : INotifyPropertyChanged
{
    private readonly ILanDiscoveryService _discoveryService;
    private readonly IChatService _chatService;
    private int _groupUnreadCount;

    /// <summary>List of currently online peers.</summary>
    public ObservableCollection<Peer> OnlinePeers { get; } = new();

    /// <summary>Number of unread messages in the group chat.</summary>
    public int GroupUnreadCount
    {
        get => _groupUnreadCount;
        set => SetProperty(ref _groupUnreadCount, value);
    }

    /// <summary>The local user's display name.</summary>
    public string DisplayName => _discoveryService.DisplayName;
    
    /// <summary>The local user's IP address.</summary>
    public string LocalIP => _discoveryService.LocalIP;

    /// <summary>Command to open the group chat page.</summary>
    public ICommand OpenGroupChatCommand { get; }
    
    /// <summary>Command to open a private chat with a specific peer.</summary>
    public ICommand OpenPrivateChatCommand { get; }
    
    /// <summary>Command to manually refresh the list of peers.</summary>
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

    /// <summary>
    /// Handles incoming messages globally to increment unread badges when the user is not actively viewing a chat.
    /// </summary>
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

    /// <summary>
    /// Adds a newly discovered peer to the UI list immediately.
    /// </summary>
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

    /// <summary>
    /// Removes a peer from the UI list when they disconnect or time out.
    /// </summary>
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

    /// <summary>
    /// Fully synchronizes the local UI peer list with the current network state provided by the discovery service.
    /// </summary>
    private void OnPeersUpdated(object? sender, List<Peer> peers)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Create hashsets for O(1) lookups of IP addresses to determine diffs
            var currentIPs = OnlinePeers.Select(p => p.IPAddress).ToHashSet();
            var newIPs = peers.Select(p => p.IPAddress).ToHashSet();

            // Identify peers that dropped offline and remove them from the UI list
            var toRemove = OnlinePeers.Where(p => !newIPs.Contains(p.IPAddress)).ToList();
            foreach (var p in toRemove) OnlinePeers.Remove(p);

            // Identify newly discovered peers and append them to the UI list
            foreach (var p in peers)
            {
                if (!currentIPs.Contains(p.IPAddress))
                    OnlinePeers.Add(p);
            }
        });
    }

    /// <summary>
    /// Manually triggers a refresh of the peer list by pulling the latest active peers from the discovery service.
    /// </summary>
    private void RefreshPeers()
    {
        var peers = _discoveryService.GetActivePeers();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            OnlinePeers.Clear();
            foreach (var p in peers) OnlinePeers.Add(p);
        });
    }

    /// <summary>Resets the unread count for the group chat.</summary>
    public void ResetGroupUnread()
    {
        // Clear the notification badge when the user opens the group chat
        GroupUnreadCount = 0;
    }

    /// <summary>Resets the unread count for a specific peer.</summary>
    /// <param name="peerIP">The IP address of the peer.</param>
    public void ResetPeerUnread(string peerIP)
    {
        // Find the specific peer model and clear its individual notification badge
        var peer = OnlinePeers.FirstOrDefault(p => p.IPAddress == peerIP);
        if (peer != null)
            peer.UnreadCount = 0;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Triggers the PropertyChanged event to notify UI bindings that a property has been updated.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        // Safely invoke the PropertyChanged event if there are any UI subscribers attached
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Updates the backing field of a property and triggers a UI update only if the value actually changed.
    /// </summary>
    /// <returns>True if the value was changed; false otherwise.</returns>
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        // Prevent redundant UI updates and loops if the value hasn't actually changed
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
