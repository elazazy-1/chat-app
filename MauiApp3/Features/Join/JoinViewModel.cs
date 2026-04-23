using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp3.Services;

namespace MauiApp3.Features.Join;

public class JoinViewModel : INotifyPropertyChanged
{
    private readonly ILanDiscoveryService _discoveryService;
    private readonly IChatService _chatService;
    private string _displayName = string.Empty;
    private bool _isJoining;

    public string DisplayName
    {
        get => _displayName;
        set
        {
            SetProperty(ref _displayName, value);
            NotifyCanJoinChanged();
        }
    }

    public bool IsJoining
    {
        get => _isJoining;
        set
        {
            SetProperty(ref _isJoining, value);
            NotifyCanJoinChanged();
        }
    }

    public bool CanJoin => !string.IsNullOrWhiteSpace(DisplayName) && !IsJoining;

    public ICommand JoinCommand { get; }

    public JoinViewModel(ILanDiscoveryService discoveryService, IChatService chatService)
    {
        _discoveryService = discoveryService;
        _chatService = chatService;
        JoinCommand = new Command(async () => await JoinAsync(), () => CanJoin);
    }

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
