using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Services;

namespace MauiApp3.Features.Join;

/// <summary>
/// ViewModel for the Join screen, handling user input for display name
/// and initiating the network join process.
/// </summary>
public class JoinViewModel : INotifyPropertyChanged
{
    private readonly ILanDiscoveryService _discoveryService;
    private readonly IChatService _chatService;
    private string _displayName = string.Empty;
    private bool _isJoining;

    /// <summary>The user's chosen display name.</summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            SetProperty(ref _displayName, value);
            NotifyCanJoinChanged();
        }
    }

    /// <summary>Indicates whether the join process is currently active.</summary>
    public bool IsJoining
    {
        get => _isJoining;
        set
        {
            SetProperty(ref _isJoining, value);
            NotifyCanJoinChanged();
        }
    }

    /// <summary>Determines if the join button can be clicked.</summary>
    public bool CanJoin => !string.IsNullOrWhiteSpace(DisplayName) && !IsJoining;

    /// <summary>Command executed when the user clicks the join button.</summary>
    public ICommand JoinCommand { get; }

    public JoinViewModel(ILanDiscoveryService discoveryService, IChatService chatService)
    {
        _discoveryService = discoveryService;
        _chatService = chatService;
        JoinCommand = new Command(async () => await JoinAsync(), () => CanJoin);
    }

    /// <summary>
    /// Executes the join process: starts broadcasting presence and listening for chats,
    /// then navigates to the lobby.
    /// </summary>
    private async Task JoinAsync()
    {
        if (!CanJoin) return;

        IsJoining = true;

        try
        {
            await _discoveryService.StartBroadcastingAsync(DisplayName.Trim());
            await _chatService.StartListeningAsync(5001);

            await Shell.Current.GoToAsync("//LobbyPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to join: {ex.Message}", "OK");
        }
        finally
        {
            IsJoining = false;
        }
    }

    private void NotifyCanJoinChanged()
    {
        OnPropertyChanged(nameof(CanJoin));
        if (JoinCommand is Command cmd)
            cmd.ChangeCanExecute();
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
