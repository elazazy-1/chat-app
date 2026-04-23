using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MauiApp3.Models;

namespace MauiApp3.Services;

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

    public event EventHandler<Peer>? PeerDiscovered;
    public event EventHandler<Peer>? PeerLost;
    public event EventHandler<List<Peer>>? PeersUpdated;

    public string LocalIP { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    public List<Peer> GetActivePeers()
    {
        lock (_lock)
        {
            return _peers.Values.Where(p => p.IsOnline).ToList();
        }
    }

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

    private async Task BroadcastPresenceAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var packet = JsonSerializer.Serialize(new
                {
                    name = DisplayName,
                    ip = LocalIP,
                    port = TcpPort,
                    leaving = false
                });
                var data = Encoding.UTF8.GetBytes(packet);
                _broadcastClient?.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
            }
            catch { }

            await Task.Delay(BroadcastIntervalMs, ct).ConfigureAwait(false);
        }
    }

    private async Task ListenForPeersAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_listenerClient == null) break;
                var result = await _listenerClient.ReceiveAsync(ct);
                var json = Encoding.UTF8.GetString(result.Buffer);
                var packet = JsonSerializer.Deserialize<DiscoveryPacket>(json);

                if (packet == null || packet.ip == LocalIP) continue;

                if (packet.leaving)
                {
                    RemovePeer(packet.ip);
                }
                else
                {
                    AddOrUpdatePeer(packet);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private void AddOrUpdatePeer(DiscoveryPacket packet)
    {
        bool isNew = false;
        lock (_lock)
        {
            if (_peers.TryGetValue(packet.ip, out var existing))
            {
                existing.LastSeen = DateTime.Now;
                existing.Name = packet.name;
                if (!existing.IsOnline)
                {
                    existing.IsOnline = true;
                    isNew = true;
                }
            }
            else
            {
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
            var peer = _peers[packet.ip];
            PeerDiscovered?.Invoke(this, peer);
            PeersUpdated?.Invoke(this, GetActivePeers());
        }
    }

    private void RemovePeer(string ip)
    {
        lock (_lock)
        {
            if (_peers.TryGetValue(ip, out var peer))
            {
                peer.IsOnline = false;
                PeerLost?.Invoke(this, peer);
                PeersUpdated?.Invoke(this, GetActivePeers());
            }
        }
    }

    private async Task CleanupStalePeersAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(5000, ct).ConfigureAwait(false);

            List<string> staleIPs;
            lock (_lock)
            {
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

    private static string GetLocalIPAddress()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        return addr.Address.ToString();
                    }
                }
            }

            // Fallback
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
        _cts?.Cancel();
        _broadcastClient?.Dispose();
        _listenerClient?.Dispose();
    }

    private record DiscoveryPacket(string name, string ip, int port, bool leaving);
}
