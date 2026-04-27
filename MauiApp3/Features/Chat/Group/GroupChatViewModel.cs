using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Helpers;
using MauiApp3.Models;
using MauiApp3.Services;

namespace MauiApp3.Features.Chat.Group;

/// <summary>
/// ViewModel for the Group Chat screen. Handles sending/receiving messages,
/// files, audio recordings, and tracking peer join/leave events.
/// </summary>
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

    /// <summary>
    /// Handles incoming messages from the ChatService, appending group messages to the UI.
    /// </summary>
    private void OnMessageReceived(object? sender, ChatMessage msg)
    {
        if (!msg.IsGroupMessage) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Messages.Add(msg);
        });
    }

    /// <summary>
    /// Injects a system message into the chat when a new peer joins the LAN.
    /// </summary>
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

    /// <summary>
    /// Injects a system message into the chat when a peer drops off the LAN.
    /// </summary>
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

    /// <summary>
    /// Constructs and broadcasts a standard text message to the group.
    /// </summary>
    private async Task SendTextMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        // Construct the message object with our metadata
        var msg = new ChatMessage
        {
            SenderName = _discoveryService.DisplayName,
            SenderIP = _discoveryService.LocalIP,
            Content = MessageText.Trim(),
            MessageType = MessageType.Text,
            IsGroupMessage = true,
            IsMine = true // Flags this message to render on the right side of the UI
        };

        // Instantly add to local UI for responsiveness
        Messages.Add(msg);
        MessageText = string.Empty;

        // Transmit to all other peers asynchronously
        await _chatService.BroadcastMessageAsync(msg);
    }

    /// <summary>
    /// Opens the native file picker and broadcasts the selected file to the group.
    /// </summary>
    private async Task AttachFileAsync()
    {
        try
        {
            // Open the native OS file picker
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a file to send"
            });

            // User canceled the picker
            if (result == null) return;

            // Transmit the selected file to the entire group
            await _chatService.BroadcastFileAsync(result.FullPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"File pick error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles the microphone recording state and broadcasts the audio when stopped.
    /// </summary>
    private async Task ToggleRecordingAsync()
    {
        if (IsRecording)
        {
            // We were already recording, so stop the recorder and retrieve the generated WAV data
            IsRecording = false;
            var audioData = await _audioService.StopRecordingAsync();
            if (audioData != null && audioData.Length > 0)
            {
                // Broadcast the successfully captured voice message to everyone in the group
                await _chatService.BroadcastAudioAsync(audioData);
            }
        }
        else
        {
            // We are not recording, so request microphone permissions and start capturing audio
            var started = await _audioService.StartRecordingAsync();
            if (started)
            {
                IsRecording = true;
            }
        }
    }

    /// <summary>
    /// Plays the embedded audio data from a voice message.
    /// </summary>
    private async Task PlayAudioAsync(ChatMessage msg)
    {
        if (msg?.FileData != null)
        {
            await _audioService.PlayAudioAsync(msg.FileData);
        }
    }

    /// <summary>
    /// Saves the embedded file data from a message to the local device storage.
    /// </summary>
    private async Task SaveFileAsync(ChatMessage msg)
    {
        if (msg?.FileData == null || string.IsNullOrEmpty(msg.FileName)) return;

        try
        {
            // Ensure the filename doesn't contain invalid characters or paths
            var safeFileName = Path.GetFileName(msg.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                safeFileName = "file";

            // Delegate to the platform-specific download helper to save the byte array
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
        // Safely invoke the PropertyChanged event if there are any UI subscribers attached
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        // Prevent redundant UI updates and loops if the value hasn't actually changed
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
