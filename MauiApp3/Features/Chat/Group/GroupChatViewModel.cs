using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Helpers;
using MauiApp3.Models;
using MauiApp3.Services;

namespace MauiApp3.Features.Chat.Group;

public class GroupChatViewModel : INotifyPropertyChanged
{
    private readonly IChatService _chatService;
    private readonly ILanDiscoveryService _discoveryService;
    private readonly IAudioRecorderService _audioService;
    private readonly ChatHistoryService _historyService;
    private string _messageText = string.Empty;
    private bool _isEmojiPanelVisible;
    private bool _isRecording;

    public ObservableCollection<ChatMessage> Messages => _historyService.GroupMessages;
    public string[] Emojis => EmojiData.All;

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

    public GroupChatViewModel(IChatService chatService, ILanDiscoveryService discoveryService, IAudioRecorderService audioService, ChatHistoryService historyService)
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
        _discoveryService.PeerDiscovered += OnPeerJoined;
        _discoveryService.PeerLost += OnPeerLeft;
    }

    private void OnMessageReceived(object? sender, ChatMessage msg)
    {
        if (!msg.IsGroupMessage) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(msg);
        });
    }

    private void OnPeerJoined(object? sender, Peer peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new ChatMessage
            {
                MessageType = MessageType.System,
                Content = $"{peer.Name} joined the chat",
                SenderName = "System"
            });
        });
    }

    private void OnPeerLeft(object? sender, Peer peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(new ChatMessage
            {
                MessageType = MessageType.System,
                Content = $"{peer.Name} left the chat",
                SenderName = "System"
            });
        });
    }

    private async Task SendTextMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        var msg = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            Content = MessageText.Trim(),
            MessageType = MessageType.Text,
            IsGroupMessage = true,
            IsMine = true
        };

        Messages.Add(msg);
        MessageText = string.Empty;

        await _chatService.BroadcastMessageAsync(msg);
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

            await _chatService.BroadcastFileAsync(result.FullPath);
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
                await _chatService.BroadcastAudioAsync(audioData);
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
