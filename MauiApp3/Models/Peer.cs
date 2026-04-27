using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiApp3.Models;

/// <summary>
/// Represents a remote user (peer) discovered on the local network.
/// Implements INotifyPropertyChanged to support UI data binding.
/// </summary>
public class Peer : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _ipAddress = string.Empty;
    private int _port = 5001;
    private DateTime _lastSeen = DateTime.Now;
    private int _unreadCount;
    private bool _isOnline = true;

    /// <summary>The display name of the peer.</summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>The IP address of the peer.</summary>
    public string IPAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    /// <summary>The port the peer is listening on.</summary>
    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    /// <summary>The last time the peer was seen online/sent a heartbeat.</summary>
    public DateTime LastSeen
    {
        get => _lastSeen;
        set => SetProperty(ref _lastSeen, value);
    }

    /// <summary>The number of unread private messages from this peer.</summary>
    public int UnreadCount
    {
        get => _unreadCount;
        set => SetProperty(ref _unreadCount, value);
    }

    /// <summary>Indicates whether the peer is currently considered online.</summary>
    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(ref _isOnline, value);
    }

    /// <summary>Gets a 1-2 character initials string representing the peer's name.</summary>
    public string Initials => string.IsNullOrWhiteSpace(Name) ? "?" :
        Name.Length >= 2 ? Name[..2].ToUpper() : Name[..1].ToUpper();

    /// <summary>Event triggered when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Helper method to set a property value and trigger the PropertyChanged event if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="backingStore">Reference to the backing field of the property.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">The name of the property (automatically provided by CallerMemberName).</param>
    /// <returns>True if the value was changed, false if it was already the same.</returns>
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
