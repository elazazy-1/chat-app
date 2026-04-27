using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MauiApp3.Models;

namespace MauiApp3.Services;

/// <summary>
/// Service responsible for finding other peers on the local network
/// using UDP broadcasting and listening for discovery packets.
/// </summary>
public class LanDiscoveryService : ILanDiscoveryService, IDisposable
{
    private const int DiscoveryPort = 5000;
    private const int BroadcastIntervalMs = 3000;
    private const int PeerTimeoutMs = 10000;
    private const int TcpPort = 5001;

    private UdpClient? _broadcastClient;
    private UdpClient? _listenerClient;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<string, Peer> _peers = new();
    private readonly object _lock = new();

    /// <summary>Event raised when a new peer joins the network.</summary>
    public event EventHandler<Peer>? PeerDiscovered;
    
    /// <summary>Event raised when a peer leaves or times out.</summary>
    public event EventHandler<Peer>? PeerLost;
    
    /// <summary>Event raised whenever the list of active peers changes.</summary>
    public event EventHandler<List<Peer>>? PeersUpdated;

    public string LocalIP { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current list of online peers.
    /// </summary>
    /// <returns>A list of active Peer objects.</returns>
    public List<Peer> GetActivePeers()
    {
        lock (_lock)
        {
            return _peers.Values.Where(p => p.IsOnline).ToList();
        }
    }

    /// <summary>
    /// Starts broadcasting the device's presence and listening for other peers.
    /// </summary>
    /// <param name="displayName">The name to broadcast to others.</param>
    public async Task StartBroadcastingAsync(string displayName)
    {
        DisplayName = displayName;
        LocalIP = GetLocalIPAddress();

        _cts = new CancellationTokenSource();

        _listenerClient = new UdpClient();
        _listenerClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listenerClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

        _broadcastClient = new UdpClient();
        _broadcastClient.EnableBroadcast = true;

        _ = Task.Run(() => ListenForPeersAsync(_cts.Token));
        _ = Task.Run(() => BroadcastPresenceAsync(_cts.Token));
        _ = Task.Run(() => CleanupStalePeersAsync(_cts.Token));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops broadcasting and listening, and sends a "leaving" packet to other peers.
    /// </summary>
    public async Task StopBroadcastingAsync()
    {
        try
        {
            var leavePacket = JsonSerializer.Serialize(new
            {
                name = DisplayName,
                ip = LocalIP,
                port = TcpPort,
                leaving = true
            });
            var data = Encoding.UTF8.GetBytes(leavePacket);
            _broadcastClient?.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
        }
        catch { }

        _cts?.Cancel();
        await Task.Delay(100);

        _broadcastClient?.Close();
        _broadcastClient?.Dispose();
        _listenerClient?.Close();
        _listenerClient?.Dispose();

        _broadcastClient = null;
        _listenerClient = null;
    }

    /// <summary>
    /// Background loop that periodically broadcasts our presence on the LAN via UDP.
    /// </summary>
    private async Task BroadcastPresenceAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Create a lightweight JSON packet containing our basic connectivity info
                var packet = JsonSerializer.Serialize(new
                {
                    name = DisplayName,
                    ip = LocalIP,
                    port = TcpPort,
                    leaving = false // Flag indicating we are actively online
                });
                var data = Encoding.UTF8.GetBytes(packet);
                
                // Broadcast on the subnet 255.255.255.255 to the designated UDP port
                _broadcastClient?.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            }
            catch { }

            await Task.Delay(BroadcastIntervalMs, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Background loop that listens for UDP broadcast packets from other peers on the LAN.
    /// </summary>
    private async Task ListenForPeersAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_listenerClient == null) break;

                // Wait for any UDP packet on the discovery port
                var result = await _listenerClient.ReceiveAsync(ct);
                var json = Encoding.UTF8.GetString(result.Buffer);
                var packet = JsonSerializer.Deserialize<DiscoveryPacket>(json);

                // Ignore malformed packets or packets sent by our own IP
                if (packet == null || packet.ip == LocalIP) continue;

                if (packet.leaving)
                {
                    // A peer has explicitly signaled they are shutting down
                    RemovePeer(packet.ip);
                }
                else
                {
                    // A peer has broadcasted a heartbeat
                    AddOrUpdatePeer(packet);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    /// <summary>
    /// Processes an incoming discovery packet, either adding a new peer or updating the LastSeen timestamp of an existing one.
    /// </summary>
    private void AddOrUpdatePeer(DiscoveryPacket packet)
    {
        bool isNew = false;
        lock (_lock)
        {
            if (_peers.TryGetValue(packet.ip, out var existing))
            {
                // Update the last seen timestamp to prevent timeout
                existing.LastSeen = DateTime.Now;
                existing.Name = packet.name;

                // If the peer was previously marked offline, restore them
                if (!existing.IsOnline)
                {
                    existing.IsOnline = true;
                    isNew = true;
                }
            }
            else
            {
                // We've never seen this IP before, instantiate a new Peer model
                var peer = new Peer
                {
                    Name = packet.name,
                    IPAddress = packet.ip,
                    Port = packet.port,
                    LastSeen = DateTime.Now,
                    IsOnline = true
                };
                _peers[packet.ip] = peer;
                isNew = true;
            }
        }

        if (isNew)
        {
            // Fire events to notify the UI about the newly joined peer
            var peer = _peers[packet.ip];
            PeerDiscovered?.Invoke(this, peer);
            PeersUpdated?.Invoke(this, GetActivePeers());
        }
    }

    /// <summary>
    /// Marks a peer as offline and triggers the PeerLost event.
    /// </summary>
    private void RemovePeer(string ip)
    {
        lock (_lock)
        {
            if (_peers.TryGetValue(ip, out var peer))
            {
                // Mark as offline and emit events so the UI can remove them from the lobby list
                peer.IsOnline = false;
                PeerLost?.Invoke(this, peer);
                PeersUpdated?.Invoke(this, GetActivePeers());
            }
        }
    }

    /// <summary>
    /// Background loop that periodically purges peers that haven't sent a heartbeat packet recently.
    /// </summary>
    private async Task CleanupStalePeersAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Periodically wake up to evaluate peer freshness
            await Task.Delay(5000, ct).ConfigureAwait(false);

            List<string> staleIPs;
            lock (_lock)
            {
                // Find peers who haven't sent a heartbeat packet recently
                staleIPs = _peers.Values
                    .Where(p => p.IsOnline && (DateTime.Now - p.LastSeen).TotalMilliseconds > PeerTimeoutMs)
                    .Select(p => p.IPAddress)
                    .ToList();
            }

            foreach (var ip in staleIPs)
            {
                RemovePeer(ip);
            }
        }
    }

    /// <summary>
    /// Attempts to dynamically discover the most appropriate local IPv4 address for the active network interface.
    /// </summary>
    private static string GetLocalIPAddress()
    {
        try
        {
            // Iterate through all active network interfaces to find the local WiFi/LAN adapter
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    // Prefer IPv4 non-loopback addresses
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        return addr.Address.ToString();
                    }
                }
            }

            // Fallback strategy: open a dummy UDP connection to Google DNS.
            // This tricks the OS routing table into returning our preferred local IP address.
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
                return endPoint.Address.ToString();
        }
        catch { }
        return "127.0.0.1";
    }

    public void Dispose()
    {
        // Cancel all async loops and free the underlying UDP socket resources
        _cts?.Cancel();
        _broadcastClient?.Dispose();
        _listenerClient?.Dispose();
    }

    private record DiscoveryPacket(string name, string ip, int port, bool leaving);
}
