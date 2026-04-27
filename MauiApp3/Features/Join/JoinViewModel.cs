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
        if (!CanJoin) return; // Prevent double joins if the button is mashed

        IsJoining = true;

        try
        {
            // Start the UDP broadcasting loop to announce our presence on the LAN
            await _discoveryService.StartBroadcastingAsync(DisplayName.Trim());
            
            // Start the TCP listener to accept incoming messages and files
            await _chatService.StartListeningAsync(5001);

            // Navigation is successful, move the user into the main lobby
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

    /// <summary>
    /// Notifies the UI and the JoinCommand to re-evaluate whether the user is allowed to join the lobby.
    /// </summary>
    private void NotifyCanJoinChanged()
    {
        // Notify the UI that the CanJoin property might have changed
        OnPropertyChanged(nameof(CanJoin));
        
        // Also explicitly notify the ICommand so bound buttons re-evaluate their enabled state
        if (JoinCommand is Command cmd)
            cmd.ChangeCanExecute();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Triggers the PropertyChanged event to notify UI bindings that a property has been updated.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        // Safely invoke the PropertyChanged event if there are any UI subscribers attached
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Updates the backing field of a property and triggers a UI update only if the value actually changed.
    /// </summary>
    /// <returns>True if the value was changed; false otherwise.</returns>
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        // Prevent redundant UI updates and loops if the value hasn't actually changed
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
