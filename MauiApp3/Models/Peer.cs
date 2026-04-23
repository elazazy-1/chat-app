using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp3.Models;

public class Peer : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _ipAddress = string.Empty;
    private int _port = 5001;
    private DateTime _lastSeen = DateTime.Now;
    private int _unreadCount;
    private bool _isOnline = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string IPAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public DateTime LastSeen
    {
        get => _lastSeen;
        set => SetProperty(ref _lastSeen, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set => SetProperty(ref _unreadCount, value);
    }

    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }

    public string Initials => string.IsNullOrWhiteSpace(Name) ? "?" :
        Name.Length >= 2 ? Name[..2].ToUpper() : Name[..1].ToUpper();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
