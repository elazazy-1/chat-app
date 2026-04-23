using MauiApp3.Models;

namespace MauiApp3.Services;

public interface ILanDiscoveryService
{
    event EventHandler<Peer>? PeerDiscovered;
    event EventHandler<Peer>? PeerLost;
    event EventHandler<List<Peer>>? PeersUpdated;

    string LocalIP { get; }
    string DisplayName { get; }
    List<Peer> GetActivePeers();
    Task StartBroadcastingAsync(string displayName);
    Task StopBroadcastingAsync();
}
