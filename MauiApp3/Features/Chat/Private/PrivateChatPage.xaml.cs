using System.Collections.Specialized;
using MauiApp3.Features.Lobby;
using MauiApp3.Models;

namespace MauiApp3.Features.Chat.Private;

/// <summary>
/// The code-behind for the Private Chat page, handling UI lifecycle events,
/// scrolling behaviors, and workarounds for CollectionView rendering issues.
/// </summary>
public partial class PrivateChatPage : ContentPage
{
    private readonly LobbyViewModel _lobbyVm;

    private double _lastHeight;
    private NotifyCollectionChangedEventHandler? _collectionChangedHandler;

    public PrivateChatPage(PrivateChatViewModel viewModel, LobbyViewModel lobbyVm)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _lobbyVm = lobbyVm;
        SizeChanged += OnSizeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is PrivateChatViewModel vm)
        {
            _lobbyVm.ResetPeerUnread(vm.PeerIP);

            // Workaround: MAUI CollectionView sometimes fails to render items
            // when bound to an ObservableCollection, especially the first item(s).
            // We force a re-bind whenever the page appears to ensure freshness.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var source = MessagesCollection.ItemsSource;
                    MessagesCollection.ItemsSource = null;
                    MessagesCollection.ItemsSource = source;
                }
                catch { }
            });

            _collectionChangedHandler = (s, e) => OnMessagesCollectionChanged(s, e, vm);
            vm.Messages.CollectionChanged += _collectionChangedHandler;
        }
        _lastHeight = Height;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (BindingContext is PrivateChatViewModel vm)
        {
            if (_collectionChangedHandler != null)
            {
                vm.Messages.CollectionChanged -= _collectionChangedHandler;
                _collectionChangedHandler = null;
            }
            vm.Dispose();
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e, PrivateChatViewModel vm)
    {
        // Workaround: when the collection goes from empty to having items,
        // force a refresh because the CollectionView sometimes doesn't render the first item.
        if (e.Action == NotifyCollectionChangedAction.Add && vm.Messages.Count <= 2)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                try
                {
                    var source = MessagesCollection.ItemsSource;
                    MessagesCollection.ItemsSource = null;
                    MessagesCollection.ItemsSource = source;

                    if (e.NewItems?.Count > 0 && e.NewItems[0] is ChatMessage msg)
                    {
                        MessagesCollection.ScrollTo(msg, position: ScrollToPosition.End, animate: false);
                    }
                }
                catch { }
            });
        }
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (_lastHeight > 0 && Height < _lastHeight - 50)
        {
            ScrollToEnd();
        }
        _lastHeight = Height;
    }

    private void ScrollToEnd()
    {
        if (BindingContext is PrivateChatViewModel vm && vm.Messages.Count > 0)
        {
            var last = vm.Messages[^1];
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    MessagesCollection.ScrollTo(last, position: ScrollToPosition.End, animate: false);
                }
                catch { }
            });
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
