using MauiApp3.Models;

namespace MauiApp3.Services;

/// <summary>
/// Interface for discovering peers on the local area network (LAN) using UDP broadcasting.
/// </summary>
public interface ILanDiscoveryService
{
    /// <summary>Event raised when a new peer is discovered.</summary>
    event EventHandler<Peer>? PeerDiscovered;
    
    /// <summary>Event raised when a previously known peer is lost (no heartbeat).</summary>
    event EventHandler<Peer>? PeerLost;
    
    /// <summary>Event raised when the list of active peers is updated.</summary>
    event EventHandler<List<Peer>>? PeersUpdated;

    /// <summary>Gets the local IP address of the device.</summary>
    string LocalIP { get; }
    
    /// <summary>Gets the display name of the current user.</summary>
    string DisplayName { get; }
    
    /// <summary>Gets a list of currently active (discovered) peers.</summary>
    List<Peer> GetActivePeers();
    
    /// <summary>Starts broadcasting the device's presence on the network.</summary>
    Task StartBroadcastingAsync(string displayName);
    
    /// <summary>Stops broadcasting the device's presence.</summary>
    Task StopBroadcastingAsync();
}
