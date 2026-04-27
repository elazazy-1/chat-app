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

    /// <summary>
    /// Background loop that continuously accepts incoming TCP connections.
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_listener == null) break;
                
                // Wait asynchronously for an incoming TCP connection
                var client = await _listener.AcceptTcpClientAsync(ct);
                
                // Fire and forget: handle the client communication on a background thread
                // so we don't block the loop from accepting the next connection
                _ = Task.Run(() => HandleClientAsync(client, ct));
            }
            catch (OperationCanceledException) { break; } // Expected during graceful shutdown
            catch { } // Ignore random socket exceptions during teardown
        }
    }

    /// <summary>
    /// Handles an individual connected TCP client, reading the payload size and then the full JSON message.
    /// </summary>
    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            using var stream = client.GetStream();
            
            // Protocol expects a 4-byte integer length prefix before the payload
            var lengthBuffer = new byte[4];
            await ReadExactAsync(stream, lengthBuffer, 4, ct);
            int length = BitConverter.ToInt32(lengthBuffer, 0);

            // Basic sanity check to prevent malicious huge payloads crashing memory (limit to 50MB)
            if (length <= 0 || length > 50_000_000) return; 

            // Allocate buffer and read the exact payload length from the stream
            var dataBuffer = new byte[length];
            await ReadExactAsync(stream, dataBuffer, length, ct);

            // Deserialize the JSON payload back into a ChatMessage object
            var json = Encoding.UTF8.GetString(dataBuffer);
            var message = JsonSerializer.Deserialize<ChatMessage>(json);

            if (message != null)
            {
                message.IsMine = false; // Explicitly tag as remote
                MessageReceived?.Invoke(this, message);
            }
        }
        catch { }
        finally
        {
            client.Close();
        }
    }

    /// <summary>
    /// Helper method to guarantee that exactly the specified number of bytes is read from the stream.
    /// </summary>
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

    /// <summary>
    /// Sends a generic chat message directly to a specific peer.
    /// </summary>
    public async Task SendMessageAsync(Peer peer, ChatMessage message)
    {
        await SendToPeerAsync(peer, message);
    }

    /// <summary>
    /// Broadcasts a chat message concurrently to all currently active peers.
    /// </summary>
    public async Task BroadcastMessageAsync(ChatMessage message)
    {
        // Get all online peers and spawn a concurrent Task to send the message to each
        var peers = _discoveryService.GetActivePeers();
        var tasks = peers.Select(p => SendToPeerAsync(p, message));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Reads a file from disk and sends it as a chat message. Can be broadcast or targeted to a specific peer.
    /// </summary>
    public async Task SendFileAsync(Peer peer, string filePath, bool isGroupMessage)
    {
        // Load the file into memory
        var fileData = await System.IO.File.ReadAllBytesAsync(filePath);

        // Construct the file message payload
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

        // Determine if this is a broadcast or a targeted private transfer
        if (isGroupMessage)
            await BroadcastMessageAsync(message);
        else
            await SendToPeerAsync(peer, message);

        // Trigger local UI update
        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    /// <summary>
    /// Reads a file from disk and broadcasts it to all peers in the group.
    /// </summary>
    public async Task BroadcastFileAsync(string filePath)
    {
        // Load the file into memory
        var fileData = await System.IO.File.ReadAllBytesAsync(filePath);

        // Construct the file message payload
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

        // Broadcast to all peers concurrently
        await BroadcastMessageAsync(message);

        // Trigger local UI update
        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    /// <summary>
    /// Packages captured audio bytes into a chat message and sends it.
    /// </summary>
    public async Task SendAudioAsync(Peer peer, byte[] audioData, bool isGroupMessage)
    {
        // Construct the audio message payload
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

        // Determine if this is a broadcast or a targeted private transfer
        if (isGroupMessage)
            await BroadcastMessageAsync(message);
        else
            await SendToPeerAsync(peer, message);

        // Trigger local UI update
        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    /// <summary>
    /// Packages captured audio bytes into a chat message and broadcasts it to all peers.
    /// </summary>
    public async Task BroadcastAudioAsync(byte[] audioData)
    {
        // Construct the audio message payload
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

        // Broadcast to all peers concurrently
        await BroadcastMessageAsync(message);

        // Trigger local UI update
        message.IsMine = true;
        MessageReceived?.Invoke(this, message);
    }

    /// <summary>
    /// Core TCP sender method. Connects to the peer, prepends the payload length, and sends the JSON data.
    /// </summary>
    private async Task SendToPeerAsync(Peer peer, ChatMessage message)
    {
        try
        {
            using var client = new TcpClient();
            
            // Set reasonable timeouts so we don't hang indefinitely if the peer suddenly drops off the network
            client.SendTimeout = 5000;
            client.ReceiveTimeout = 5000;
            await client.ConnectAsync(peer.IPAddress, peer.Port);

            using var stream = client.GetStream();
            
            // Serialize message and prefix the byte array with its length
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);
            var lengthPrefix = BitConverter.GetBytes(data.Length);

            // Send length first, then the actual payload
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
        // Cancel the socket accept loop and close the TCP listener explicitly
        _cts?.Cancel();
        _listener?.Stop();
    }
}
