using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Helpers;
using MauiApp3.Models;
using MauiApp3.Services;

namespace MauiApp3.Features.Chat.Private;

/// <summary>
/// ViewModel for the Private Chat screen. Handles sending/receiving messages,
/// files, and audio recordings specifically targeted to a single peer.
/// </summary>
[QueryProperty(nameof(PeerIP), "peerIP")]
[QueryProperty(nameof(PeerName), "peerName")]
public class PrivateChatViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IChatService _chatService;
    private readonly ILanDiscoveryService _discoveryService;
    private readonly IAudioRecorderService _audioService;
    private readonly ChatHistoryService _historyService;
    private string _messageText = string.Empty;
    private bool _isEmojiPanelVisible;
    private bool _isRecording;
    private string _peerIP = string.Empty;
    private string _peerName = string.Empty;
    private bool _disposed;

    public ObservableCollection<ChatMessage> Messages => _historyService.GetPrivateMessages(PeerIP);
    public string[] Emojis => EmojiData.All;

    public string PeerIP
    {
        get => _peerIP;
        set
        {
            SetProperty(ref _peerIP, value);
            OnPropertyChanged(nameof(Messages));
        }
    }

    public string PeerName
    {
        get => _peerName;
        set => SetProperty(ref _peerName, value);
    }

    public string MessageText
    {
        get => _messageText;
        set => SetProperty(ref _messageText, value);
    }

    public bool IsEmojiPanelVisible
    {
        get => _isEmojiPanelVisible;
        set => SetProperty(ref _isEmojiPanelVisible, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    public ICommand SendMessageCommand { get; }
    public ICommand ToggleEmojiCommand { get; }
    public ICommand InsertEmojiCommand { get; }
    public ICommand AttachFileCommand { get; }
    public ICommand ToggleRecordingCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand SaveFileCommand { get; }

    public PrivateChatViewModel(IChatService chatService, ILanDiscoveryService discoveryService, IAudioRecorderService audioService, ChatHistoryService historyService)
    {
        _chatService = chatService;
        _discoveryService = discoveryService;
        _audioService = audioService;
        _historyService = historyService;

        SendMessageCommand = new Command(async () => await SendTextMessageAsync());
        ToggleEmojiCommand = new Command(() => IsEmojiPanelVisible = !IsEmojiPanelVisible);
        InsertEmojiCommand = new Command<string>(emoji =>
        {
            MessageText += emoji;
        });
        AttachFileCommand = new Command(async () => await AttachFileAsync());
        ToggleRecordingCommand = new Command(async () => await ToggleRecordingAsync());
        PlayAudioCommand = new Command<ChatMessage>(async (msg) => await PlayAudioAsync(msg));
        SaveFileCommand = new Command<ChatMessage>(async (msg) => await SaveFileAsync(msg));

        _chatService.MessageReceived += OnMessageReceived;
    }

    private Peer? GetTargetPeer()
    {
        return _discoveryService.GetActivePeers().FirstOrDefault(p => p.IPAddress == PeerIP)
               ?? new Peer { IPAddress = PeerIP, Name = PeerName, Port = 5001 };
    }

    private void OnMessageReceived(object? sender, ChatMessage msg)
    {
        if (msg.IsGroupMessage) return;
        if (msg.SenderIP != PeerIP && !msg.IsMine) return;
        if (msg.IsMine && msg.TargetIP != PeerIP) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(msg);
        });
    }

    private async Task SendTextMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        var peer = GetTargetPeer();
        if (peer == null) return;

        var msg = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            Content = MessageText.Trim(),
            MessageType = MessageType.Text,
            IsGroupMessage = false,
            TargetIP = PeerIP,
            IsMine = true
        };

        Messages.Add(msg);
        MessageText = string.Empty;

        await _chatService.SendMessageAsync(peer, msg);
    }

    private async Task AttachFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a file to send"
            });

            if (result == null) return;
            var peer = GetTargetPeer();
            if (peer == null) return;

            await _chatService.SendFileAsync(peer, result.FullPath, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"File pick error: {ex.Message}");
        }
    }

    private async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            IsRecording = false;
            var audioData = await _audioService.StopRecordingAsync();
            if (audioData != null && audioData.Length > 0)
            {
                var peer = GetTargetPeer();
                if (peer != null)
                {
                    await _chatService.SendAudioAsync(peer, audioData, false);
                }
            }
        }
        else
        {
            var started = await _audioService.StartRecordingAsync();
            if (started)
            {
                IsRecording = true;
            }
        }
    }

    private async Task PlayAudioAsync(ChatMessage msg)
    {
        if (msg?.FileData != null)
        {
            await _audioService.PlayAudioAsync(msg.FileData);
        }
    }

    private async Task SaveFileAsync(ChatMessage msg)
    {
        if (msg?.FileData == null || string.IsNullOrEmpty(msg.FileName)) return;

        try
        {
            var safeFileName = Path.GetFileName(msg.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                safeFileName = "file";

            var targetPath = await DownloadPathHelper.SaveBytesAsync(safeFileName, msg.FileData);
            await Shell.Current.DisplayAlert("File Saved", $"Saved to: {targetPath}", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _chatService.MessageReceived -= OnMessageReceived;
        }
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
