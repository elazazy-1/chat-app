using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MauiApp3.Models;

namespace MauiApp3.Services;

/// <summary>
/// Service responsible for TCP communication, enabling sending and receiving
/// text, files, and audio messages between peers.
/// </summary>
public class ChatService : IChatService, IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ILanDiscoveryService _discoveryService;

    /// <summary>Event raised when a new chat message is received.</summary>
    public event EventHandler<ChatMessage>? MessageReceived;

    public ChatService(ILanDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    /// <summary>
    /// Starts the TCP listener to accept incoming connections on a specific port.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    public async Task StartListeningAsync(int port)
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();

        _ = Task.Run(() => AcceptClientsAsync(_cts.Token));
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops the TCP listener and cancels any pending operations.
    /// </summary>
    public async Task StopListeningAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        await Task.Delay(100);
    }

    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_listener == null) break;
                var client = await _listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(client, ct));
            }
            catch (OperationCanceledException) { break; }
            catch { }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            using var stream = client.GetStream();
            // Read length prefix (4 bytes)
            var lengthBuffer = new byte[4];
            await ReadExactAsync(stream, lengthBuffer, 4, ct);
            int length = BitConverter.ToInt32(lengthBuffer, 0);

            if (length <= 0 || length > 50_000_000) return; // Max 50MB

            var dataBuffer = new byte[length];
            await ReadExactAsync(stream, dataBuffer, length, ct);

            var json = Encoding.UTF8.GetString(dataBuffer);
            var message = JsonSerializer.Deserialize<ChatMessage>(json);

            if (message != null)
            {
                message.IsMine = false;
                MessageReceived?.Invoke(this, message);
            }
        }
        catch { }
        finally
        {
            client.Close();
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0) throw new IOException("Connection closed");
            offset += read;
        }
    }

    public async Task SendMessageAsync(Peer peer, ChatMessage message)
    {
        await SendToPeerAsync(peer, message);
    }

    public async Task BroadcastMessageAsync(ChatMessage message)
    {
        var peers = _discoveryService.GetActivePeers();
        var tasks = peers.Select(p => SendToPeerAsync(p, message));
        await Task.WhenAll(tasks);
    }

    public async Task SendFileAsync(Peer peer, string filePath, bool isGroupMessage)
    {
        var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
        var message = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            MessageType = MessageType.File,
            FileName = Path.GetFileName(filePath),
            FileData = fileData,
            Content = $"Sent file: {Path.GetFileName(filePath)}",
            IsGroupMessage = isGroupMessage,
            TargetIP = isGroupMessage ? null : peer.IPAddress
        };

        if (isGroupMessage)
            await BroadcastMessageAsync(message);
        else
            await SendToPeerAsync(peer, message);

        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    public async Task BroadcastFileAsync(string filePath)
    {
        var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
        var message = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            MessageType = MessageType.File,
            FileName = Path.GetFileName(filePath),
            FileData = fileData,
            Content = $"Sent file: {Path.GetFileName(filePath)}",
            IsGroupMessage = true
        };

        await BroadcastMessageAsync(message);
        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    public async Task SendAudioAsync(Peer peer, byte[] audioData, bool isGroupMessage)
    {
        var message = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            MessageType = MessageType.Audio,
            FileName = "voice_message.wav",
            FileData = audioData,
            Content = "Voice message",
            IsGroupMessage = isGroupMessage,
            TargetIP = isGroupMessage ? null : peer.IPAddress
        };

        if (isGroupMessage)
            await BroadcastMessageAsync(message);
        else
            await SendToPeerAsync(peer, message);

        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    public async Task BroadcastAudioAsync(byte[] audioData)
    {
        var message = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            MessageType = MessageType.Audio,
            FileName = "voice_message.wav",
            FileData = audioData,
            Content = "Voice message",
            IsGroupMessage = true
        };

        await BroadcastMessageAsync(message);
        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    private async Task SendToPeerAsync(Peer peer, ChatMessage message)
    {
        try
        {
            using var client = new TcpClient();
            client.SendTimeout = 5000;
            client.ReceiveTimeout = 5000;
            await client.ConnectAsync(peer.IPAddress, peer.Port);

            using var stream = client.GetStream();
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            var lengthPrefix = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(lengthPrefix);
            await stream.WriteAsync(data);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send to {peer.IPAddress}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Stop();
    }
}
